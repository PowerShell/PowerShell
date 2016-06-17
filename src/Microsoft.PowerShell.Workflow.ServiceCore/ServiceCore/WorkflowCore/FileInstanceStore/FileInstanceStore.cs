/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

namespace Microsoft.PowerShell.Workflow
{
    using System;
    using System.Runtime.DurableInstancing;
    using System.Activities.DurableInstancing;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using System.IO;
    using System.Xml;
    using System.Management.Automation.Tracing;
    using System.Activities.Persistence;

    internal class FileInstanceStore : InstanceStore
    {
        private static readonly Tracer StructuredTracer = new Tracer();

        private readonly PSWorkflowFileInstanceStore _stores;
        internal FileInstanceStore(PSWorkflowFileInstanceStore stores)
        {
            _stores = stores;
        }

        protected override IAsyncResult BeginTryCommand(InstancePersistenceContext context, InstancePersistenceCommand command, TimeSpan timeout, AsyncCallback callback, object state)
        {
            StructuredTracer.Correlate();

            try
            {
                if (command is SaveWorkflowCommand)
                {
                    return new TypedCompletedAsyncResult<bool>(SaveWorkflow(context, (SaveWorkflowCommand)command), callback, state);
                }
                else if (command is LoadWorkflowCommand)
                {
                    return new TypedCompletedAsyncResult<bool>(LoadWorkflow(context, (LoadWorkflowCommand)command), callback, state);
                }
                else if (command is CreateWorkflowOwnerCommand)
                {
                    return new TypedCompletedAsyncResult<bool>(CreateWorkflowOwner(context, (CreateWorkflowOwnerCommand)command), callback, state);
                }
                else if (command is DeleteWorkflowOwnerCommand)
                {
                    return new TypedCompletedAsyncResult<bool>(DeleteWorkflowOwner(context, (DeleteWorkflowOwnerCommand)command), callback, state);
                }
                return new TypedCompletedAsyncResult<bool>(false, callback, state);
            }
            catch (Exception e)
            {
                return new TypedCompletedAsyncResult<Exception>(e, callback, state);
            }
        }
        
        protected override bool EndTryCommand(IAsyncResult result)
        {
            StructuredTracer.Correlate();

            TypedCompletedAsyncResult<Exception> exceptionResult = result as TypedCompletedAsyncResult<Exception>;
            if (exceptionResult != null)
            {
                throw exceptionResult.Data;
            }
            return TypedCompletedAsyncResult<bool>.End(result);
        }
        
        private bool SaveWorkflow(InstancePersistenceContext context, SaveWorkflowCommand command)
        {
            if (context.InstanceVersion == -1)
            {
                context.BindAcquiredLock(0);
            }

            if (command.CompleteInstance)
            {
                context.CompletedInstance();
            }
            else
            {
                string instanceType = "";

                const string InstanceTypeXName = "{urn:schemas-microsoft-com:System.Runtime.DurableInstancing/4.0/metadata}InstanceType";
                InstanceValue instanceTypeInstanceValue;
                if (command.InstanceMetadataChanges.TryGetValue(InstanceTypeXName, out instanceTypeInstanceValue))
                {
                    instanceType = instanceTypeInstanceValue.Value.ToString();
                }

                Dictionary<string, object> fullInstanceData = new Dictionary<string, object>();
                fullInstanceData.Add("instanceId", context.InstanceView.InstanceId);
                fullInstanceData.Add("instanceOwnerId", context.InstanceView.InstanceOwner.InstanceOwnerId);
                fullInstanceData.Add("instanceData", SerializeablePropertyBag(command.InstanceData));
                fullInstanceData.Add("instanceMetadata", SerializeInstanceMetadata(context, command));

                foreach (KeyValuePair<XName, InstanceValue> property in command.InstanceMetadataChanges)
                {
                    context.WroteInstanceMetadataValue(property.Key, property.Value);
                }

                context.PersistedInstance(command.InstanceData);

                _stores.Save(WorkflowStoreComponents.Definition
                                                | WorkflowStoreComponents.Metadata
                                                | WorkflowStoreComponents.Streams
                                                | WorkflowStoreComponents.TerminatingError
                                                | WorkflowStoreComponents.Timer
                                                | WorkflowStoreComponents.ActivityState 
                                                | WorkflowStoreComponents.JobState,
                                                fullInstanceData);

            }

            return true;
        }
        
        private bool LoadWorkflow(InstancePersistenceContext context, LoadWorkflowCommand command)
        {
            if (command.AcceptUninitializedInstance)
            {
                return false;
            }

            if (context.InstanceVersion == -1)
            {
                context.BindAcquiredLock(0);
            }

            Guid instanceId = context.InstanceView.InstanceId;
            Guid instanceOwnerId = context.InstanceView.InstanceOwner.InstanceOwnerId;

            IDictionary<XName, InstanceValue> instanceData = null;
            IDictionary<XName, InstanceValue> instanceMetadata = null;

            Dictionary<string, object> fullInstanceData = _stores.LoadWorkflowContext();
            
            instanceData = this.DeserializePropertyBag((Dictionary<XName, object>)fullInstanceData["instanceData"]);
            instanceMetadata = this.DeserializePropertyBag((Dictionary<XName, object>)fullInstanceData["instanceMetadata"]);

            context.LoadedInstance(InstanceState.Initialized, instanceData, instanceMetadata, null, null);

            return true;
        }
        
        private bool CreateWorkflowOwner(InstancePersistenceContext context, CreateWorkflowOwnerCommand command)
        {
            Guid instanceOwnerId = Guid.NewGuid();
            context.BindInstanceOwner(instanceOwnerId, instanceOwnerId);
            context.BindEvent(HasRunnableWorkflowEvent.Value);
            return true;
        }
        
        private bool DeleteWorkflowOwner(InstancePersistenceContext context, DeleteWorkflowOwnerCommand command)
        {
            return true;
        }
        
        private Dictionary<XName, object> SerializeablePropertyBag(IDictionary<XName, InstanceValue> source)
        {
            Dictionary<XName, object> scratch = new Dictionary<XName, object>();
            foreach (KeyValuePair<XName, InstanceValue> property in source)
            {
                bool writeOnly = (property.Value.Options & InstanceValueOptions.WriteOnly) != 0;

                if (!writeOnly && !property.Value.IsDeletedValue)
                {
                    scratch.Add(property.Key, property.Value.Value);
                }
            }

            return scratch;
        }
        private Dictionary<XName, object> SerializeInstanceMetadata(InstancePersistenceContext context, SaveWorkflowCommand command)
        {
            Dictionary<XName, object> metadata = null;

            foreach (var property in command.InstanceMetadataChanges)
            {
                if (!property.Value.Options.HasFlag(InstanceValueOptions.WriteOnly))
                {
                    if (metadata == null)
                    {
                        metadata = new Dictionary<XName, object>();
                        // copy current metadata. note that we must rid of InstanceValue as it is not properly serializeable
                        foreach (var m in context.InstanceView.InstanceMetadata)
                        {
                            metadata.Add(m.Key, m.Value.Value);
                        }
                    }

                    if (metadata.ContainsKey(property.Key))
                    {
                        if (property.Value.IsDeletedValue) metadata.Remove(property.Key);
                        else metadata[property.Key] = property.Value.Value;
                    }
                    else
                    {
                        if (!property.Value.IsDeletedValue) metadata.Add(property.Key, property.Value.Value);
                    }
                }
            }

            if (metadata == null)
                metadata = new Dictionary<XName, object>();

            return metadata;
        }

        private IDictionary<XName, InstanceValue> DeserializePropertyBag(Dictionary<XName, object> source)
        {
            Dictionary<XName, InstanceValue> destination = new Dictionary<XName, InstanceValue>();

            foreach (KeyValuePair<XName, object> property in source)
            {
                destination.Add(property.Key, new InstanceValue(property.Value));
            }

            return destination;
        }


    }
}
 