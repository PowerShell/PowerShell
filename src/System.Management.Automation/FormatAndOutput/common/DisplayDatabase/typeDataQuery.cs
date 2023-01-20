// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation;
using System.Text;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    internal static class DisplayCondition
    {
        internal static bool Evaluate(PSObject obj, PSPropertyExpression ex, out PSPropertyExpressionResult expressionResult)
        {
            expressionResult = null;
            List<PSPropertyExpressionResult> res = ex.GetValues(obj);
            if (res.Count == 0)
                return false;
            if (res[0].Exception != null)
            {
                expressionResult = res[0];
                return false;
            }

            return LanguagePrimitives.IsTrue(res[0].Result);
        }
    }

    /// <summary>
    /// Helper object holding a generic object and the related
    /// "applies to" object.
    /// It is used in by the inheritance based type match algorithm.
    /// </summary>
    internal sealed class TypeMatchItem
    {
        internal TypeMatchItem(object obj, AppliesTo a)
        {
            Item = obj;
            AppliesTo = a;
        }

        internal TypeMatchItem(object obj, AppliesTo a, PSObject currentObject)
        {
            Item = obj;
            AppliesTo = a;
            CurrentObject = currentObject;
        }

        internal object Item { get; }

        internal AppliesTo AppliesTo { get; }

        internal PSObject CurrentObject { get; }
    }

    /// <summary>
    /// Algorithm to execute a type match on a list of entities
    /// having an "applies to" associated object.
    /// </summary>
    internal sealed class TypeMatch
    {
        #region tracer

        [TraceSource("TypeMatch", "F&O TypeMatch")]
        private static readonly PSTraceSource s_classTracer =
            PSTraceSource.GetTracer("TypeMatch", "F&O TypeMatch");

        private static PSTraceSource s_activeTracer = null;

        private static PSTraceSource ActiveTracer
        {
            get
            {
                return s_activeTracer ?? s_classTracer;
            }
        }

        internal static void SetTracer(PSTraceSource t)
        {
            s_activeTracer = t;
        }

        internal static void ResetTracer()
        {
            s_activeTracer = s_classTracer;
        }
        #endregion tracer

        internal TypeMatch(PSPropertyExpressionFactory expressionFactory, TypeInfoDataBase db, Collection<string> typeNames)
        {
            _expressionFactory = expressionFactory;
            _db = db;
            _typeNameHierarchy = typeNames;
            _useInheritance = true;
        }

        internal TypeMatch(PSPropertyExpressionFactory expressionFactory, TypeInfoDataBase db, Collection<string> typeNames, bool useInheritance)
        {
            _expressionFactory = expressionFactory;
            _db = db;
            _typeNameHierarchy = typeNames;
            _useInheritance = useInheritance;
        }

        internal bool PerfectMatch(TypeMatchItem item)
        {
            int match = ComputeBestMatch(item.AppliesTo, item.CurrentObject);
            if (match == BestMatchIndexUndefined)
                return false;

            if (_bestMatchIndex == BestMatchIndexUndefined ||
                match < _bestMatchIndex)
            {
                _bestMatchIndex = match;
                _bestMatchItem = item;
            }

            return _bestMatchIndex == BestMatchIndexPerfect;
        }

        internal object BestMatch
        {
            get
            {
                if (_bestMatchItem == null)
                    return null;
                return _bestMatchItem.Item;
            }
        }

        private int ComputeBestMatch(AppliesTo appliesTo, PSObject currentObject)
        {
            int best = BestMatchIndexUndefined;
            foreach (TypeOrGroupReference r in appliesTo.referenceList)
            {
                PSPropertyExpression ex = null;
                if (r.conditionToken != null)
                {
                    ex = _expressionFactory.CreateFromExpressionToken(r.conditionToken);
                }

                int currentMatch = BestMatchIndexUndefined;
                TypeReference tr = r as TypeReference;

                if (tr != null)
                {
                    // we have a type
                    currentMatch = MatchTypeIndex(tr.name, currentObject, ex);
                }
                else
                {
                    // we have a type group reference
                    TypeGroupReference tgr = r as TypeGroupReference;

                    // find the type group definition the reference points to
                    TypeGroupDefinition tgd = DisplayDataQuery.FindGroupDefinition(_db, tgr.name);

                    if (tgd != null)
                    {
                        // we found the group, see if the group has the type
                        currentMatch = ComputeBestMatchInGroup(tgd, currentObject, ex);
                    }
                }

                if (currentMatch == BestMatchIndexPerfect)
                    return currentMatch;

                if (best == BestMatchIndexUndefined || best < currentMatch)
                {
                    best = currentMatch;
                }
            }

            return best;
        }

        private int ComputeBestMatchInGroup(TypeGroupDefinition tgd, PSObject currentObject, PSPropertyExpression ex)
        {
            int best = BestMatchIndexUndefined;
            int k = 0;
            foreach (TypeReference tr in tgd.typeReferenceList)
            {
                int currentMatch = MatchTypeIndex(tr.name, currentObject, ex);
                if (currentMatch == BestMatchIndexPerfect)
                    return currentMatch;

                if (best == BestMatchIndexUndefined || best < currentMatch)
                {
                    best = currentMatch;
                }

                k++;
            }

            return best;
        }

        private int MatchTypeIndex(string typeName, PSObject currentObject, PSPropertyExpression ex)
        {
            if (string.IsNullOrEmpty(typeName))
                return BestMatchIndexUndefined;
            int k = 0;
            foreach (string name in _typeNameHierarchy)
            {
                if (string.Equals(name, typeName, StringComparison.OrdinalIgnoreCase)
                            && MatchCondition(currentObject, ex))
                {
                    return k;
                }

                if (k == 0 && !_useInheritance)
                    break;
                k++;
            }

            return BestMatchIndexUndefined;
        }

        private static bool MatchCondition(PSObject currentObject, PSPropertyExpression ex)
        {
            if (ex == null)
                return true;

            PSPropertyExpressionResult expressionResult;
            bool retVal = DisplayCondition.Evaluate(currentObject, ex, out expressionResult);

            return retVal;
        }

        private readonly PSPropertyExpressionFactory _expressionFactory;
        private readonly TypeInfoDataBase _db;
        private readonly Collection<string> _typeNameHierarchy;
        private readonly bool _useInheritance;

        private int _bestMatchIndex = BestMatchIndexUndefined;
        private TypeMatchItem _bestMatchItem;

        private const int BestMatchIndexUndefined = -1;
        private const int BestMatchIndexPerfect = 0;
    }

    internal static class DisplayDataQuery
    {
        #region tracer

        [TraceSource("DisplayDataQuery", "DisplayDataQuery")]
        private static readonly PSTraceSource s_classTracer =
            PSTraceSource.GetTracer("DisplayDataQuery", "DisplayDataQuery");

        private static PSTraceSource s_activeTracer = null;

        private static PSTraceSource ActiveTracer
        {
            get
            {
                return s_activeTracer ?? s_classTracer;
            }
        }

        internal static void SetTracer(PSTraceSource t)
        {
            s_activeTracer = t;
        }

        internal static void ResetTracer()
        {
            s_activeTracer = s_classTracer;
        }
        #endregion tracer

        internal static EnumerableExpansion GetEnumerableExpansionFromType(PSPropertyExpressionFactory expressionFactory, TypeInfoDataBase db, Collection<string> typeNames)
        {
            TypeMatch match = new TypeMatch(expressionFactory, db, typeNames);
            foreach (EnumerableExpansionDirective expansionDirective in db.defaultSettingsSection.enumerableExpansionDirectiveList)
            {
                if (match.PerfectMatch(new TypeMatchItem(expansionDirective, expansionDirective.appliesTo)))
                {
                    return expansionDirective.enumerableExpansion;
                }
            }

            if (match.BestMatch != null)
            {
                return ((EnumerableExpansionDirective)(match.BestMatch)).enumerableExpansion;
            }
            else
            {
                Collection<string> typesWithoutPrefix = Deserializer.MaskDeserializationPrefix(typeNames);
                if (typesWithoutPrefix != null)
                {
                    EnumerableExpansion result = GetEnumerableExpansionFromType(expressionFactory, db, typesWithoutPrefix);
                    return result;
                }

                // return a default value if no matches were found
                return EnumerableExpansion.EnumOnly;
            }
        }

        internal static FormatShape GetShapeFromType(PSPropertyExpressionFactory expressionFactory, TypeInfoDataBase db, Collection<string> typeNames)
        {
            ShapeSelectionDirectives shapeDirectives = db.defaultSettingsSection.shapeSelectionDirectives;

            TypeMatch match = new TypeMatch(expressionFactory, db, typeNames);
            foreach (FormatShapeSelectionOnType shapeSelOnType in shapeDirectives.formatShapeSelectionOnTypeList)
            {
                if (match.PerfectMatch(new TypeMatchItem(shapeSelOnType, shapeSelOnType.appliesTo)))
                {
                    return shapeSelOnType.formatShape;
                }
            }

            if (match.BestMatch != null)
            {
                return ((FormatShapeSelectionOnType)(match.BestMatch)).formatShape;
            }
            else
            {
                Collection<string> typesWithoutPrefix = Deserializer.MaskDeserializationPrefix(typeNames);
                if (typesWithoutPrefix != null)
                {
                    FormatShape result = GetShapeFromType(expressionFactory, db, typesWithoutPrefix);
                    return result;
                }

                // return a default value if no matches were found
                return FormatShape.Undefined;
            }
        }

        internal static FormatShape GetShapeFromPropertyCount(TypeInfoDataBase db, int propertyCount)
        {
            if (propertyCount <= db.defaultSettingsSection.shapeSelectionDirectives.PropertyCountForTable)
                return FormatShape.Table;

            return FormatShape.List;
        }

        internal static ViewDefinition GetViewByShapeAndType(PSPropertyExpressionFactory expressionFactory, TypeInfoDataBase db,
                FormatShape shape, Collection<string> typeNames, string viewName)
        {
            if (shape == FormatShape.Undefined)
            {
                return GetDefaultView(expressionFactory, db, typeNames);
            }
            // map the FormatShape to a type derived from ViewDefinition
            System.Type t = null;
            if (shape == FormatShape.Table)
            {
                t = typeof(TableControlBody);
            }
            else if (shape == FormatShape.List)
            {
                t = typeof(ListControlBody);
            }
            else if (shape == FormatShape.Wide)
            {
                t = typeof(WideControlBody);
            }
            else if (shape == FormatShape.Complex)
            {
                t = typeof(ComplexControlBody);
            }
            else
            {
                Diagnostics.Assert(false, "unknown shape: this should never happen unless a new shape is added");
                return null;
            }

            return GetView(expressionFactory, db, t, typeNames, viewName);
        }

        internal static ViewDefinition GetOutOfBandView(PSPropertyExpressionFactory expressionFactory,
                                                        TypeInfoDataBase db, Collection<string> typeNames)
        {
            TypeMatch match = new TypeMatch(expressionFactory, db, typeNames);
            foreach (ViewDefinition vd in db.viewDefinitionsSection.viewDefinitionList)
            {
                if (!IsOutOfBandView(vd))
                    continue;
                if (match.PerfectMatch(new TypeMatchItem(vd, vd.appliesTo)))
                {
                    return vd;
                }
            }

            // this is the best match we had
            ViewDefinition result = match.BestMatch as ViewDefinition;
            // we were unable to find a best match so far..try
            // to get rid of Deserialization prefix and see if a
            // match can be found.
            if (result == null)
            {
                Collection<string> typesWithoutPrefix = Deserializer.MaskDeserializationPrefix(typeNames);
                if (typesWithoutPrefix != null)
                {
                    result = GetOutOfBandView(expressionFactory, db, typesWithoutPrefix);
                }
            }

            return result;
        }

        private static ViewDefinition GetView(PSPropertyExpressionFactory expressionFactory, TypeInfoDataBase db, System.Type mainControlType, Collection<string> typeNames, string viewName)
        {
            TypeMatch match = new TypeMatch(expressionFactory, db, typeNames);
            foreach (ViewDefinition vd in db.viewDefinitionsSection.viewDefinitionList)
            {
                if (vd == null || mainControlType != vd.mainControl.GetType())
                {
                    ActiveTracer.WriteLine(
                        "NOT MATCH {0}  NAME: {1}",
                        ControlBase.GetControlShapeName(vd.mainControl), (vd != null ? vd.name : string.Empty));
                    continue;
                }

                if (IsOutOfBandView(vd))
                {
                    ActiveTracer.WriteLine(
                        "NOT MATCH OutOfBand {0}  NAME: {1}",
                        ControlBase.GetControlShapeName(vd.mainControl), vd.name);
                    continue;
                }

                if (vd.appliesTo == null)
                {
                    ActiveTracer.WriteLine(
                        "NOT MATCH {0}  NAME: {1}  No applicable types",
                        ControlBase.GetControlShapeName(vd.mainControl), vd.name);
                    continue;
                }
                // first make sure we match on name:
                // if not, we do not try a match at all
                if (viewName != null && !string.Equals(vd.name, viewName, StringComparison.OrdinalIgnoreCase))
                {
                    ActiveTracer.WriteLine(
                        "NOT MATCH {0}  NAME: {1}",
                        ControlBase.GetControlShapeName(vd.mainControl), vd.name);
                    continue;
                }

                // check if we have a perfect match
                // if so, we are done
                try
                {
                    TypeMatch.SetTracer(ActiveTracer);
                    if (match.PerfectMatch(new TypeMatchItem(vd, vd.appliesTo)))
                    {
                        TraceHelper(vd, true);
                        return vd;
                    }
                }
                finally
                {
                    TypeMatch.ResetTracer();
                }

                TraceHelper(vd, false);
            }

            // this is the best match we had
            ViewDefinition result = GetBestMatch(match);

            // we were unable to find a best match so far..try
            // to get rid of Deserialization prefix and see if a
            // match can be found.
            if (result == null)
            {
                Collection<string> typesWithoutPrefix = Deserializer.MaskDeserializationPrefix(typeNames);
                if (typesWithoutPrefix != null)
                {
                    result = GetView(expressionFactory, db, mainControlType, typesWithoutPrefix, viewName);
                }
            }

            return result;
        }

        private static void TraceHelper(ViewDefinition vd, bool isMatched)
        {
            if ((ActiveTracer.Options & PSTraceSourceOptions.WriteLine) != 0)
            {
                foreach (TypeOrGroupReference togr in vd.appliesTo.referenceList)
                {
                    StringBuilder sb = new StringBuilder();
                    TypeReference tr = togr as TypeReference;
                    sb.Append(isMatched ? "MATCH FOUND" : "NOT MATCH");
                    if (tr != null)
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture, $" {ControlBase.GetControlShapeName(vd.mainControl)} NAME: {vd.name}  TYPE: {tr.name}");
                    }
                    else
                    {
                        TypeGroupReference tgr = togr as TypeGroupReference;
                        sb.AppendFormat(CultureInfo.InvariantCulture, $" {ControlBase.GetControlShapeName(vd.mainControl)} NAME: {vd.name}  GROUP: {tgr.name}");
                    }

                    ActiveTracer.WriteLine(sb.ToString());
                }
            }
        }

        private static ViewDefinition GetBestMatch(TypeMatch match)
        {
            ViewDefinition bestMatchedVD = match.BestMatch as ViewDefinition;
            if (bestMatchedVD != null)
            {
                TraceHelper(bestMatchedVD, true);
            }

            return bestMatchedVD;
        }

        private static ViewDefinition GetDefaultView(PSPropertyExpressionFactory expressionFactory, TypeInfoDataBase db, Collection<string> typeNames)
        {
            TypeMatch match = new TypeMatch(expressionFactory, db, typeNames);

            foreach (ViewDefinition vd in db.viewDefinitionsSection.viewDefinitionList)
            {
                if (vd == null)
                    continue;

                if (IsOutOfBandView(vd))
                {
                    ActiveTracer.WriteLine(
                        "NOT MATCH OutOfBand {0}  NAME: {1}",
                        ControlBase.GetControlShapeName(vd.mainControl), vd.name);
                    continue;
                }

                if (vd.appliesTo == null)
                {
                    ActiveTracer.WriteLine(
                        "NOT MATCH {0}  NAME: {1}  No applicable types",
                        ControlBase.GetControlShapeName(vd.mainControl), vd.name);
                    continue;
                }

                try
                {
                    TypeMatch.SetTracer(ActiveTracer);
                    if (match.PerfectMatch(new TypeMatchItem(vd, vd.appliesTo)))
                    {
                        TraceHelper(vd, true);
                        return vd;
                    }
                }
                finally
                {
                    TypeMatch.ResetTracer();
                }

                TraceHelper(vd, false);
            }
            // this is the best match we had
            ViewDefinition result = GetBestMatch(match);
            // we were unable to find a best match so far..try
            // to get rid of Deserialization prefix and see if a
            // match can be found.
            if (result == null)
            {
                Collection<string> typesWithoutPrefix = Deserializer.MaskDeserializationPrefix(typeNames);
                if (typesWithoutPrefix != null)
                {
                    result = GetDefaultView(expressionFactory, db, typesWithoutPrefix);
                }
            }

            return result;
        }

        private static bool IsOutOfBandView(ViewDefinition vd)
        {
            return (vd.mainControl is ComplexControlBody || vd.mainControl is ListControlBody) && vd.outOfBand;
        }

        /// <summary>
        /// Given an appliesTo list, it finds all the types that are contained (following type
        /// group references)
        /// </summary>
        /// <param name="db">Database to use.</param>
        /// <param name="appliesTo">Object to lookup.</param>
        /// <returns></returns>
        internal static AppliesTo GetAllApplicableTypes(TypeInfoDataBase db, AppliesTo appliesTo)
        {
            var allTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (TypeOrGroupReference r in appliesTo.referenceList)
            {
                // if it is a type reference, just add the type name
                TypeReference tr = r as TypeReference;
                if (tr != null)
                {
                    if (!allTypes.Contains(tr.name))
                        allTypes.Add(tr.name);
                }
                else
                {
                    // check if we have a type group reference
                    if (!(r is TypeGroupReference tgr))
                        continue;

                    // find the type group definition the reference points to
                    TypeGroupDefinition tgd = FindGroupDefinition(db, tgr.name);

                    if (tgd == null)
                        continue;

                    // we found the group, go over it
                    foreach (TypeReference x in tgd.typeReferenceList)
                    {
                        if (!allTypes.Contains(x.name))
                            allTypes.Add(x.name);
                    }
                }
            }

            AppliesTo retVal = new AppliesTo();
            foreach (string x in allTypes)
            {
                retVal.AddAppliesToType(x);
            }

            return retVal;
        }

        internal static TypeGroupDefinition FindGroupDefinition(TypeInfoDataBase db, string groupName)
        {
            foreach (TypeGroupDefinition tgd in db.typeGroupSection.typeGroupDefinitionList)
            {
                if (string.Equals(tgd.name, groupName, StringComparison.OrdinalIgnoreCase))
                    return tgd;
            }

            return null;
        }

        internal static ControlBody ResolveControlReference(TypeInfoDataBase db, List<ControlDefinition> viewControlDefinitionList,
                                                            ControlReference controlReference)
        {
            // first tri to resolve the reference at the view level
            ControlBody controlBody = ResolveControlReferenceInList(controlReference,
                viewControlDefinitionList);
            if (controlBody != null)
                return controlBody;

            // fall back to the global definitions
            return ResolveControlReferenceInList(controlReference, db.formatControlDefinitionHolder.controlDefinitionList);
        }

        private static ControlBody ResolveControlReferenceInList(ControlReference controlReference,
                                        List<ControlDefinition> controlDefinitionList)
        {
            foreach (ControlDefinition x in controlDefinitionList)
            {
                if (x.controlBody.GetType() != controlReference.controlType)
                    continue;
                if (string.Equals(controlReference.name, x.name, StringComparison.OrdinalIgnoreCase))
                    return x.controlBody;
            }

            return null;
        }
    }
}
