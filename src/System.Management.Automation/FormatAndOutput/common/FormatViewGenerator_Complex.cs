// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    internal sealed class ComplexViewGenerator : ViewGenerator
    {
        internal override void Initialize(TerminatingErrorContext errorContext, PSPropertyExpressionFactory expressionFactory,
            PSObject so, TypeInfoDataBase db, FormattingCommandLineParameters parameters)
        {
            base.Initialize(errorContext, expressionFactory, so, db, parameters);
            this.inputParameters = parameters;
        }

        internal override FormatStartData GenerateStartData(PSObject so)
        {
            FormatStartData startFormat = base.GenerateStartData(so);
            startFormat.shapeInfo = new ComplexViewHeaderInfo();
            return startFormat;
        }

        internal override FormatEntryData GeneratePayload(PSObject so, int enumerationLimit)
        {
            FormatEntryData fed = new FormatEntryData();

            if (this.dataBaseInfo.view != null)
                fed.formatEntryInfo = GenerateComplexViewEntryFromDataBaseInfo(so, enumerationLimit);
            else
                fed.formatEntryInfo = GenerateComplexViewEntryFromProperties(so, enumerationLimit);
            return fed;
        }

        private ComplexViewEntry GenerateComplexViewEntryFromProperties(PSObject so, int enumerationLimit)
        {
            ComplexViewObjectBrowser browser = new ComplexViewObjectBrowser(this.ErrorManager, this.expressionFactory, enumerationLimit);
            return browser.GenerateView(so, this.inputParameters);
        }

        private ComplexViewEntry GenerateComplexViewEntryFromDataBaseInfo(PSObject so, int enumerationLimit)
        {
            // execute on the format directive
            ComplexViewEntry cve = new ComplexViewEntry();

            // NOTE: we set a max depth to protect ourselves from infinite loops
            const int maxTreeDepth = 50;
            ComplexControlGenerator controlGenerator =
                            new ComplexControlGenerator(this.dataBaseInfo.db,
                                    this.dataBaseInfo.view.loadingInfo,
                                    this.expressionFactory,
                                    this.dataBaseInfo.view.formatControlDefinitionHolder.controlDefinitionList,
                                    this.ErrorManager,
                                    enumerationLimit,
                                    this.errorContext);

            controlGenerator.GenerateFormatEntries(maxTreeDepth,
                this.dataBaseInfo.view.mainControl, so, cve.formatValueList);
            return cve;
        }
    }

    /// <summary>
    /// Class to process a complex control directive and generate
    /// the corresponding formatting tokens.
    /// </summary>
    internal sealed class ComplexControlGenerator
    {
        internal ComplexControlGenerator(TypeInfoDataBase dataBase,
                                            DatabaseLoadingInfo loadingInfo,
                                            PSPropertyExpressionFactory expressionFactory,
                                            List<ControlDefinition> controlDefinitionList,
                                            FormatErrorManager resultErrorManager,
                                            int enumerationLimit,
                                            TerminatingErrorContext errorContext)
        {
            _db = dataBase;
            _loadingInfo = loadingInfo;
            _expressionFactory = expressionFactory;
            _controlDefinitionList = controlDefinitionList;
            _errorManager = resultErrorManager;
            _enumerationLimit = enumerationLimit;
            _errorContext = errorContext;
        }

        internal void GenerateFormatEntries(int maxTreeDepth, ControlBase control,
                PSObject so, List<FormatValue> formatValueList)
        {
            if (control == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(control));
            }

            ExecuteFormatControl(new TraversalInfo(0, maxTreeDepth), control,
                                    so, formatValueList);
        }

        private bool ExecuteFormatControl(TraversalInfo level, ControlBase control,
        PSObject so, List<FormatValue> formatValueList)
        {
            // we are looking for a complex control to execute
            ComplexControlBody complexBody = null;

            // we might have a reference
            if (control is ControlReference controlReference && controlReference.controlType == typeof(ComplexControlBody))
            {
                // retrieve the reference
                complexBody = DisplayDataQuery.ResolveControlReference(
                                        _db,
                                        _controlDefinitionList,
                                        controlReference) as ComplexControlBody;
            }
            else
            {
                // try as an in line control
                complexBody = control as ComplexControlBody;
            }

            // finally, execute the control body
            if (complexBody != null)
            {
                // we have an inline control, just execute it
                ExecuteFormatControlBody(level, so, complexBody, formatValueList);
                return true;
            }

            return false;
        }

        private void ExecuteFormatControlBody(TraversalInfo level,
                PSObject so, ComplexControlBody complexBody, List<FormatValue> formatValueList)
        {
            ComplexControlEntryDefinition activeControlEntryDefinition =
                    GetActiveComplexControlEntryDefinition(complexBody, so);

            ExecuteFormatTokenList(level,
                                 so, activeControlEntryDefinition.itemDefinition.formatTokenList, formatValueList);
        }

        private ComplexControlEntryDefinition GetActiveComplexControlEntryDefinition(ComplexControlBody complexBody, PSObject so)
        {
            // see if we have an override that matches
            var typeNames = so.InternalTypeNames;
            TypeMatch match = new TypeMatch(_expressionFactory, _db, typeNames);
            foreach (ComplexControlEntryDefinition x in complexBody.optionalEntryList)
            {
                if (match.PerfectMatch(new TypeMatchItem(x, x.appliesTo, so)))
                {
                    return x;
                }
            }

            if (match.BestMatch != null)
            {
                return match.BestMatch as ComplexControlEntryDefinition;
            }
            else
            {
                Collection<string> typesWithoutPrefix = Deserializer.MaskDeserializationPrefix(typeNames);
                if (typesWithoutPrefix != null)
                {
                    match = new TypeMatch(_expressionFactory, _db, typesWithoutPrefix);
                    foreach (ComplexControlEntryDefinition x in complexBody.optionalEntryList)
                    {
                        if (match.PerfectMatch(new TypeMatchItem(x, x.appliesTo)))
                        {
                            return x;
                        }
                    }

                    if (match.BestMatch != null)
                    {
                        return match.BestMatch as ComplexControlEntryDefinition;
                    }
                }

                // we do not have any override, use default
                return complexBody.defaultEntry;
            }
        }

        private void ExecuteFormatTokenList(TraversalInfo level,
                PSObject so, List<FormatToken> formatTokenList, List<FormatValue> formatValueList)
        {
            if (so == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(so));
            }

            // guard against infinite loop
            if (level.Level == level.MaxDepth)
            {
                return;
            }

            FormatEntry fe = new FormatEntry();

            formatValueList.Add(fe);
            #region foreach loop
            foreach (FormatToken t in formatTokenList)
            {
                if (t is TextToken tt)
                {
                    FormatTextField ftf = new FormatTextField();
                    ftf.text = _db.displayResourceManagerCache.GetTextTokenString(tt);
                    fe.formatValueList.Add(ftf);
                    continue;
                }

                if (t is NewLineToken newline)
                {
                    for (int i = 0; i < newline.count; i++)
                    {
                        fe.formatValueList.Add(new FormatNewLine());
                    }

                    continue;
                }

                if (t is FrameToken ft)
                {
                    // instantiate a new entry and attach a frame info object
                    FormatEntry feFrame = new FormatEntry();
                    feFrame.frameInfo = new FrameInfo();

                    // add the frame info
                    feFrame.frameInfo.firstLine = ft.frameInfoDefinition.firstLine;
                    feFrame.frameInfo.leftIndentation = ft.frameInfoDefinition.leftIndentation;
                    feFrame.frameInfo.rightIndentation = ft.frameInfoDefinition.rightIndentation;

                    // execute the list inside the frame
                    ExecuteFormatTokenList(level, so, ft.itemDefinition.formatTokenList, feFrame.formatValueList);

                    // add the frame computation results to the current format entry
                    fe.formatValueList.Add(feFrame);
                    continue;
                }
                #region CompoundPropertyToken
                if (t is CompoundPropertyToken cpt)
                {
                    if (!EvaluateDisplayCondition(so, cpt.conditionToken))
                    {
                        // token not active, skip it
                        continue;
                    }

                    // get the property from the object
                    object val = null;

                    // if no expression was specified, just use the
                    // object itself
                    if (cpt.expression == null || string.IsNullOrEmpty(cpt.expression.expressionValue))
                    {
                        val = so;
                    }
                    else
                    {
                        PSPropertyExpression ex = _expressionFactory.CreateFromExpressionToken(cpt.expression, _loadingInfo);
                        List<PSPropertyExpressionResult> resultList = ex.GetValues(so);
                        if (resultList.Count > 0)
                        {
                            val = resultList[0].Result;
                            if (resultList[0].Exception != null)
                            {
                                _errorManager.LogPSPropertyExpressionFailedResult(resultList[0], so);
                            }
                        }
                    }

                    // if the token is has a formatting string, it's a leaf node,
                    // do the formatting and we will be done
                    if (cpt.control == null || cpt.control is FieldControlBody)
                    {
                        // Since it is a leaf node we just consider it an empty string and go
                        // on with formatting
                        val ??= string.Empty;

                        FieldFormattingDirective fieldFormattingDirective = null;
                        StringFormatError formatErrorObject = null;
                        if (cpt.control != null)
                        {
                            fieldFormattingDirective = ((FieldControlBody)cpt.control).fieldFormattingDirective;
                            if (fieldFormattingDirective != null && _errorManager.DisplayFormatErrorString)
                            {
                                formatErrorObject = new StringFormatError();
                            }
                        }

                        IEnumerable e = PSObjectHelper.GetEnumerable(val);
                        FormatPropertyField fpf = new FormatPropertyField();
                        if (cpt.enumerateCollection && e != null)
                        {
                            foreach (object x in e)
                            {
                                if (x == null)
                                {
                                    // nothing to process
                                    continue;
                                }

                                fpf = new FormatPropertyField();

                                fpf.propertyValue = PSObjectHelper.FormatField(fieldFormattingDirective, x, _enumerationLimit, formatErrorObject, _expressionFactory);
                                fe.formatValueList.Add(fpf);
                            }
                        }
                        else
                        {
                            fpf = new FormatPropertyField();

                            fpf.propertyValue = PSObjectHelper.FormatField(fieldFormattingDirective, val, _enumerationLimit, formatErrorObject, _expressionFactory);
                            fe.formatValueList.Add(fpf);
                        }

                        if (formatErrorObject != null && formatErrorObject.exception != null)
                        {
                            _errorManager.LogStringFormatError(formatErrorObject);
                            fpf.propertyValue = _errorManager.FormatErrorString;
                        }
                    }
                    else
                    {
                        // An empty result that is not a leaf node should not be expanded
                        if (val == null)
                        {
                            continue;
                        }

                        IEnumerable e = PSObjectHelper.GetEnumerable(val);
                        if (cpt.enumerateCollection && e != null)
                        {
                            foreach (object x in e)
                            {
                                if (x == null)
                                {
                                    // nothing to process
                                    continue;
                                }

                                // proceed with the recursion
                                ExecuteFormatControl(level.NextLevel, cpt.control, PSObject.AsPSObject(x), fe.formatValueList);
                            }
                        }
                        else
                        {
                            // proceed with the recursion
                            ExecuteFormatControl(level.NextLevel, cpt.control, PSObjectHelper.AsPSObject(val), fe.formatValueList);
                        }
                    }
                }
                #endregion CompoundPropertyToken
            }
            #endregion foreach loop
        }

        private bool EvaluateDisplayCondition(PSObject so, ExpressionToken conditionToken)
        {
            if (conditionToken == null)
                return true;

            PSPropertyExpression ex = _expressionFactory.CreateFromExpressionToken(conditionToken, _loadingInfo);
            PSPropertyExpressionResult expressionResult;
            bool retVal = DisplayCondition.Evaluate(so, ex, out expressionResult);

            if (expressionResult != null && expressionResult.Exception != null)
            {
                _errorManager.LogPSPropertyExpressionFailedResult(expressionResult, so);
            }

            return retVal;
        }

        private readonly TypeInfoDataBase _db;
        private readonly DatabaseLoadingInfo _loadingInfo;
        private readonly PSPropertyExpressionFactory _expressionFactory;
        private readonly List<ControlDefinition> _controlDefinitionList;
        private readonly FormatErrorManager _errorManager;
        private readonly TerminatingErrorContext _errorContext;
        private readonly int _enumerationLimit;
    }

    internal sealed class TraversalInfo
    {
        internal TraversalInfo(int level, int maxDepth)
        {
            _level = level;
            _maxDepth = maxDepth;
        }

        internal int Level { get { return _level; } }

        internal int MaxDepth { get { return _maxDepth; } }

        internal TraversalInfo NextLevel
        {
            get
            {
                return new TraversalInfo(_level + 1, _maxDepth);
            }
        }

        private readonly int _level;
        private readonly int _maxDepth;
    }

    /// <summary>
    /// Class to generate a complex view from properties.
    /// </summary>
    internal sealed class ComplexViewObjectBrowser
    {
        internal ComplexViewObjectBrowser(FormatErrorManager resultErrorManager, PSPropertyExpressionFactory mshExpressionFactory, int enumerationLimit)
        {
            _errorManager = resultErrorManager;
            _expressionFactory = mshExpressionFactory;
            _enumerationLimit = enumerationLimit;
        }

        /// <summary>
        /// Given an object, generate a tree-like view
        /// of the object.
        /// </summary>
        /// <param name="so">Object to process.</param>
        /// <param name="inputParameters">Parameters from the command line.</param>
        /// <returns>Complex view entry to send to the output command.</returns>
        internal ComplexViewEntry GenerateView(PSObject so, FormattingCommandLineParameters inputParameters)
        {
            _complexSpecificParameters = (ComplexSpecificParameters)inputParameters.shapeParameters;

            int maxDepth = _complexSpecificParameters.maxDepth;
            TraversalInfo level = new TraversalInfo(0, maxDepth);

            List<MshParameter> mshParameterList = null;
            mshParameterList = inputParameters.mshParameterList;

            // create a top level entry as root of the tree
            ComplexViewEntry cve = new ComplexViewEntry();
            var typeNames = so.InternalTypeNames;
            if (TreatAsScalarType(typeNames))
            {
                FormatEntry fe = new FormatEntry();

                cve.formatValueList.Add(fe);
                DisplayRawObject(so, fe.formatValueList);
            }
            else
            {
                // check if the top level object is an enumeration
                IEnumerable e = PSObjectHelper.GetEnumerable(so);

                if (e != null)
                {
                    // let's start the traversal with an enumeration
                    FormatEntry fe = new FormatEntry();

                    cve.formatValueList.Add(fe);
                    DisplayEnumeration(e, level, fe.formatValueList);
                }
                else
                {
                    // let's start the traversal with a traversal on properties
                    DisplayObject(so, level, mshParameterList, cve.formatValueList);
                }
            }

            return cve;
        }

        private void DisplayRawObject(PSObject so, List<FormatValue> formatValueList)
        {
            FormatPropertyField fpf = new FormatPropertyField();

            StringFormatError formatErrorObject = null;
            if (_errorManager.DisplayFormatErrorString)
            {
                // we send a format error object down to the formatting calls
                // only if we want to show the formatting error strings
                formatErrorObject = new StringFormatError();
            }

            fpf.propertyValue = PSObjectHelper.SmartToString(so, _expressionFactory, _enumerationLimit, formatErrorObject);

            if (formatErrorObject != null && formatErrorObject.exception != null)
            {
                // if we did not have any errors in the expression evaluation
                // we might have errors in the formatting, if present
                _errorManager.LogStringFormatError(formatErrorObject);
                if (_errorManager.DisplayFormatErrorString)
                {
                    fpf.propertyValue = _errorManager.FormatErrorString;
                }
            }

            formatValueList.Add(fpf);
            formatValueList.Add(new FormatNewLine());
        }

        /// <summary>
        /// Recursive call to display an object.
        /// </summary>
        /// <param name="so">Object to display.</param>
        /// <param name="currentLevel">Current level in the traversal.</param>
        /// <param name="parameterList">List of parameters from the command line.</param>
        /// <param name="formatValueList">List of format tokens to add to.</param>
        private void DisplayObject(PSObject so, TraversalInfo currentLevel, List<MshParameter> parameterList,
                                        List<FormatValue> formatValueList)
        {
            // resolve the names of the properties
            List<MshResolvedExpressionParameterAssociation> activeAssociationList =
                        AssociationManager.SetupActiveProperties(parameterList, so, _expressionFactory);

            // create a format entry
            FormatEntry fe = new FormatEntry();
            formatValueList.Add(fe);

            // add the display name of the object
            string objectDisplayName = GetObjectDisplayName(so);
            if (objectDisplayName != null)
                objectDisplayName = "class " + objectDisplayName;

            AddPrologue(fe.formatValueList, "{", objectDisplayName);
            ProcessActiveAssociationList(so, currentLevel, activeAssociationList, AddIndentationLevel(fe.formatValueList));
            AddEpilogue(fe.formatValueList, "}");
        }

        private void ProcessActiveAssociationList(PSObject so,
                                TraversalInfo currentLevel,
                                List<MshResolvedExpressionParameterAssociation> activeAssociationList,
                                                    List<FormatValue> formatValueList)
        {
            foreach (MshResolvedExpressionParameterAssociation a in activeAssociationList)
            {
                FormatTextField ftf = new FormatTextField();

                ftf.text = a.ResolvedExpression.ToString() + " = ";
                formatValueList.Add(ftf);

                // compute the value of the entry
                List<PSPropertyExpressionResult> resList = a.ResolvedExpression.GetValues(so);
                object val = null;
                if (resList.Count >= 1)
                {
                    PSPropertyExpressionResult result = resList[0];
                    if (result.Exception != null)
                    {
                        _errorManager.LogPSPropertyExpressionFailedResult(result, so);
                        if (_errorManager.DisplayErrorStrings)
                        {
                            val = _errorManager.ErrorString;
                        }
                        else
                        {
                            val = string.Empty;
                        }
                    }
                    else
                    {
                        val = result.Result;
                    }
                }

                // extract the optional max depth
                TraversalInfo level = currentLevel;
                if (a.OriginatingParameter != null)
                {
                    object maxDepthKey = a.OriginatingParameter.GetEntry(FormatParameterDefinitionKeys.DepthEntryKey);
                    if (maxDepthKey != AutomationNull.Value)
                    {
                        int parameterMaxDept = (int)maxDepthKey;
                        level = new TraversalInfo(currentLevel.Level, parameterMaxDept);
                    }
                }

                IEnumerable e = null;
                if (val != null || (level.Level >= level.MaxDepth))
                    e = PSObjectHelper.GetEnumerable(val);

                if (e != null)
                {
                    formatValueList.Add(new FormatNewLine());
                    DisplayEnumeration(e, level.NextLevel, AddIndentationLevel(formatValueList));
                }
                else if (val == null || TreatAsLeafNode(val, level))
                {
                    DisplayLeaf(val, formatValueList);
                }
                else
                {
                    formatValueList.Add(new FormatNewLine());

                    // we need to go one more level down
                    DisplayObject(PSObject.AsPSObject(val), level.NextLevel, null,
                        AddIndentationLevel(formatValueList));
                }
            }
        }

        /// <summary>
        /// Recursive call to display an object.
        /// </summary>
        /// <param name="e">Enumeration to display.</param>
        /// <param name="level">Current level in the traversal.</param>
        /// <param name="formatValueList">List of format tokens to add to.</param>
        private void DisplayEnumeration(IEnumerable e, TraversalInfo level, List<FormatValue> formatValueList)
        {
            AddPrologue(formatValueList, "[", null);
            DisplayEnumerationInner(e, level, AddIndentationLevel(formatValueList));
            AddEpilogue(formatValueList, "]");

            formatValueList.Add(new FormatNewLine());
        }

        private void DisplayEnumerationInner(IEnumerable e, TraversalInfo level, List<FormatValue> formatValueList)
        {
            int enumCount = 0;
            foreach (object x in e)
            {
                if (LocalPipeline.GetExecutionContextFromTLS().CurrentPipelineStopping)
                {
                    throw new PipelineStoppedException();
                }

                if (_enumerationLimit >= 0)
                {
                    if (_enumerationLimit == enumCount)
                    {
                        DisplayLeaf(PSObjectHelper.Ellipsis, formatValueList);
                        break;
                    }

                    enumCount++;
                }

                if (TreatAsLeafNode(x, level))
                {
                    DisplayLeaf(x, formatValueList);
                }
                else
                {
                    // check if the top level object is an enumeration
                    IEnumerable e1 = PSObjectHelper.GetEnumerable(x);

                    if (e1 != null)
                    {
                        formatValueList.Add(new FormatNewLine());
                        DisplayEnumeration(e1, level.NextLevel, AddIndentationLevel(formatValueList));
                    }
                    else
                    {
                        DisplayObject(PSObjectHelper.AsPSObject(x), level.NextLevel, null, formatValueList);
                    }
                }
            }
        }

        /// <summary>
        /// Display a leaf value.
        /// </summary>
        /// <param name="val">Object to display.</param>
        /// <param name="formatValueList">List of format tokens to add to.</param>
        private void DisplayLeaf(object val, List<FormatValue> formatValueList)
        {
            FormatPropertyField fpf = new FormatPropertyField();

            fpf.propertyValue = PSObjectHelper.FormatField(null, PSObjectHelper.AsPSObject(val), _enumerationLimit, null, _expressionFactory);
            formatValueList.Add(fpf);
            formatValueList.Add(new FormatNewLine());
        }

        /// <summary>
        /// Determine if we have to stop the expansion.
        /// </summary>
        /// <param name="val">Object to verify.</param>
        /// <param name="level">Current level of recursion.</param>
        /// <returns></returns>
        private static bool TreatAsLeafNode(object val, TraversalInfo level)
        {
            if (level.Level >= level.MaxDepth || val == null)
                return true;

            return TreatAsScalarType(PSObject.GetTypeNames(val));
        }

        /// <summary>
        /// Treat as scalar check.
        /// </summary>
        /// <param name="typeNames">Name of the type to check.</param>
        /// <returns>True if it has to be treated as a scalar.</returns>
        private static bool TreatAsScalarType(Collection<string> typeNames)
        {
            return DefaultScalarTypes.IsTypeInList(typeNames);
        }

        private string GetObjectDisplayName(PSObject so)
        {
            if (_complexSpecificParameters.classDisplay == ComplexSpecificParameters.ClassInfoDisplay.none)
                return null;

            var typeNames = so.InternalTypeNames;
            if (typeNames.Count == 0)
            {
                return "PSObject";
            }

            if (_complexSpecificParameters.classDisplay == ComplexSpecificParameters.ClassInfoDisplay.shortName)
            {
                // get the last token in the full name
                string[] arr = typeNames[0].Split('.');
                if (arr.Length > 0)
                    return arr[arr.Length - 1];
            }

            return typeNames[0];
        }

        private static void AddPrologue(List<FormatValue> formatValueList, string openTag, string label)
        {
            if (label != null)
            {
                FormatTextField ftfLabel = new FormatTextField();
                ftfLabel.text = label;
                formatValueList.Add(ftfLabel);
                formatValueList.Add(new FormatNewLine());
            }

            FormatTextField ftf = new FormatTextField();
            ftf.text = openTag;
            formatValueList.Add(ftf);

            formatValueList.Add(new FormatNewLine());
        }

        private static void AddEpilogue(List<FormatValue> formatValueList, string closeTag)
        {
            FormatTextField ftf = new FormatTextField();

            ftf.text = closeTag;
            formatValueList.Add(ftf);

            formatValueList.Add(new FormatNewLine());
        }

        private List<FormatValue> AddIndentationLevel(List<FormatValue> formatValueList)
        {
            FormatEntry feFrame = new FormatEntry();
            feFrame.frameInfo = new FrameInfo();

            // add the frame info
            feFrame.frameInfo.firstLine = 0;
            feFrame.frameInfo.leftIndentation = _indentationStep;
            feFrame.frameInfo.rightIndentation = 0;
            formatValueList.Add(feFrame);

            return feFrame.formatValueList;
        }

        private ComplexSpecificParameters _complexSpecificParameters;

        /// <summary>
        /// Indentation added to each level in the recursion.
        /// </summary>
        private readonly int _indentationStep = 2;

        private readonly FormatErrorManager _errorManager;

        private readonly PSPropertyExpressionFactory _expressionFactory;

        private readonly int _enumerationLimit;
    }
}
