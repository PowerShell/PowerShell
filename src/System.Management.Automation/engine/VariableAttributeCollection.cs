// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    /// <summary>
    /// A collection of the attributes on the PSVariable object.
    /// </summary>
    internal sealed class PSVariableAttributeCollection : Collection<Attribute>
    {
        #region constructor

        /// <summary>
        /// Constructs a variable attribute collection attached to
        /// the specified variable. Whenever the attributes change
        /// the variable value is verified against the attribute.
        /// </summary>
        /// <param name="variable">
        /// The variable that needs to be verified anytime an attribute
        /// changes.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="variable"/> is null.
        /// </exception>
        internal PSVariableAttributeCollection(PSVariable variable)
        {
            if (variable == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(variable));
            }

            _variable = variable;
        }
        #endregion constructor

        #region Collection overrides

        /// <summary>
        /// Ensures that the variable that the attribute is being added to is still
        /// valid after the attribute is added.
        /// </summary>
        /// <param name="index">
        /// The zero-based index at which <paramref name="item"/> should be inserted.
        /// </param>
        /// <param name="item">
        /// The attribute being added to the collection.
        /// </param>
        /// <exception cref="ValidationMetadataException">
        /// If the new attribute causes the variable to be in an invalid state.
        /// </exception>
        /// <exception cref="ArgumentTransformationMetadataException">
        /// If the new attribute is an ArgumentTransformationAttribute and the transformation
        /// fails.
        /// </exception>
        protected override void InsertItem(int index, Attribute item)
        {
            object variableValue = VerifyNewAttribute(item);

            base.InsertItem(index, item);

            _variable.SetValueRaw(variableValue, true);
        }

        /// <summary>
        /// Ensures that the variable that the attribute is being set to is still
        /// valid after the attribute is set.
        /// </summary>
        /// <param name="index">
        /// The zero-based index at which <paramref name="item"/> should be set.
        /// </param>
        /// <param name="item">
        /// The attribute being set in the collection.
        /// </param>
        /// <exception cref="ValidationMetadataException">
        /// If the new attribute causes the variable to be in an invalid state.
        /// </exception>
        protected override void SetItem(int index, Attribute item)
        {
            object variableValue = VerifyNewAttribute(item);

            base.SetItem(index, item);

            _variable.SetValueRaw(variableValue, true);
        }
        #endregion Collection overrides

        #region private data

        /// <summary>
        /// Ordinarily, the collection checks/converts the value (by applying the attribute)
        /// when an attribute is added.  This is both slow and wrong when the attributes
        /// have already been checked/applied during parameter binding.  So if checking
        /// has already been done, this function will add the attribute without checking
        /// and possibly updating the value.
        /// </summary>
        /// <param name="item">The attribute to add.</param>
        internal void AddAttributeNoCheck(Attribute item)
        {
            base.InsertItem(this.Count, item);
        }

        /// <summary>
        /// Validates and performs any transformations that the new attribute
        /// implements.
        /// </summary>
        /// <param name="item">
        /// The new attribute to be added to the collection.
        /// </param>
        /// <returns>
        /// The new variable value. This may change from the original value if the
        /// new attribute is an ArgumentTransformationAttribute.
        /// </returns>
        private object VerifyNewAttribute(Attribute item)
        {
            object variableValue = _variable.Value;

            // Perform transformation before validating
            ArgumentTransformationAttribute argumentTransformation = item as ArgumentTransformationAttribute;
            if (argumentTransformation != null)
            {
                // Get an EngineIntrinsics instance using the context of the thread.

                ExecutionContext context = Runspaces.LocalPipeline.GetExecutionContextFromTLS();
                EngineIntrinsics engine = null;

                if (context != null)
                {
                    engine = context.EngineIntrinsics;
                }

                variableValue = argumentTransformation.TransformInternal(engine, variableValue);
            }

            if (!PSVariable.IsValidValue(variableValue, item))
            {
                ValidationMetadataException e = new ValidationMetadataException(
                    "ValidateSetFailure",
                    null,
                    Metadata.InvalidMetadataForCurrentValue,
                    _variable.Name,
                    ((_variable.Value != null) ? _variable.Value.ToString() : string.Empty));

                throw e;
            }

            return variableValue;
        }

        /// <summary>
        /// The variable whose value needs to be verified anytime
        /// the attributes change.
        /// </summary>
        private readonly PSVariable _variable;
        #endregion private data
    }
}
