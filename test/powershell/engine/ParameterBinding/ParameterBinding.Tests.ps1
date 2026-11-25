# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Compiled object pipeline parameter binding" -Tags "CI" {
    BeforeAll {
        $types = Add-Type -PassThru -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace PipelineBindingTests
{
    public class Record
    {
        public object Value;
        public object BoundValue;
        public bool IsBound;
        public string ParameterSet;
        public object[] Assignments;
        public string Label;
        public int Attempts;
    }

    public abstract class RecordCommand : PSCmdlet
    {
        private PSObject _value = new PSObject("initial");
        private readonly List<object> _assignments = new List<object>();

        public virtual PSObject Value
        {
            get { return _value; }
            set { _value = value; _assignments.Add(value); }
        }

        [Parameter]
        public string Label { get; set; }

        protected override void ProcessRecord()
        {
            WriteObject(new Record {
                Value = Value,
                IsBound = MyInvocation.BoundParameters.ContainsKey("Value"),
                BoundValue = MyInvocation.BoundParameters.ContainsKey("Value") ? MyInvocation.BoundParameters["Value"] : null,
                ParameterSet = ParameterSetName,
                Label = Label,
                Assignments = _assignments.ToArray()
            });
        }
    }

    [Cmdlet("Test", "PipelineRecord")]
    public sealed class RecordInputCommand : RecordCommand
    {
        [Parameter(ValueFromPipeline = true)]
        public override PSObject Value { get { return base.Value; } set { base.Value = value; } }
    }

    [Cmdlet("Test", "MandatoryPipelineRecord")]
    public sealed class MandatoryInputCommand : RecordCommand
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public override PSObject Value { get { return base.Value; } set { base.Value = value; } }
    }

    [Cmdlet("Test", "ObjectPipelineRecord")]
    public sealed class ObjectInputCommand : PSCmdlet
    {
        private object _value = "initial";
        private readonly List<object> _assignments = new List<object>();

        [Parameter(ValueFromPipeline = true)]
        public object Value
        {
            get { return _value; }
            set { _value = value; _assignments.Add(value); }
        }

        protected override void ProcessRecord()
        {
            WriteObject(new Record {
                Value = Value,
                IsBound = MyInvocation.BoundParameters.ContainsKey("Value"),
                BoundValue = MyInvocation.BoundParameters["Value"],
                Assignments = _assignments.ToArray()
            });
        }
    }

    [Cmdlet("Test", "ArrayPipelineRecord")]
    public class ArrayInputCommand : PSCmdlet
    {
        private PSObject[] _value = new[] { new PSObject("initial") };
        private readonly List<object> _assignments = new List<object>();

        [Parameter(ValueFromPipeline = true)]
        public virtual PSObject[] Value
        {
            get { return _value; }
            set { _value = value; _assignments.Add(value); }
        }

        protected override void ProcessRecord()
        {
            WriteObject(new Record {
                Value = Value,
                IsBound = MyInvocation.BoundParameters.ContainsKey("Value"),
                BoundValue = MyInvocation.BoundParameters["Value"],
                Assignments = _assignments.ToArray()
            });
        }
    }

    [Cmdlet("Test", "MandatoryArrayPipelineRecord")]
    public sealed class MandatoryArrayInputCommand : ArrayInputCommand
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public override PSObject[] Value { get { return base.Value; } set { base.Value = value; } }
    }

    [Cmdlet("Test", "RetryPipelineRecord")]
    public sealed class RetryInputCommand : RecordCommand
    {
        private int _attempts;

        [Parameter(ValueFromPipeline = true)]
        public override PSObject Value
        {
            get { return base.Value; }
            set
            {
                if (Equals(value?.BaseObject, "retry") && ++_attempts == 1)
                    throw new PSInvalidCastException("Retry binding with coercion.");
                base.Value = value;
            }
        }

        protected override void ProcessRecord() { WriteObject(_attempts); }
    }

    public abstract class RetryScheduleCommand : PSCmdlet
    {
        private PSObject _value = new PSObject("initial");
        private int _attempts;
        private readonly List<object> _assignments = new List<object>();

        [Parameter]
        public int RejectedAttemptCount { get; set; } = 2;

        [Parameter(ParameterSetName = "Chosen")]
        public SwitchParameter Choose { get; set; }

        public virtual PSObject Value
        {
            get { return _value; }
            set
            {
                _assignments.Add(value?.BaseObject);
                if (Equals(value?.BaseObject, "retry") && ++_attempts <= RejectedAttemptCount)
                    throw new PSInvalidCastException("Retryable setter failure.");
                _value = value;
            }
        }

        protected override void ProcessRecord()
        {
            WriteObject(new Record {
                Value = Value,
                BoundValue = MyInvocation.BoundParameters["Value"],
                IsBound = MyInvocation.BoundParameters.ContainsKey("Value"),
                ParameterSet = ParameterSetName,
                Assignments = _assignments.ToArray(),
                Attempts = _attempts
            });
        }
    }

    [Cmdlet("Test", "DefaultSetRetry", DefaultParameterSetName = "Chosen")]
    public sealed class DefaultSetRetryCommand : RetryScheduleCommand
    {
        [Parameter(ValueFromPipeline = true)]
        public override PSObject Value { get { return base.Value; } set { base.Value = value; } }
    }

    [Cmdlet("Test", "DefaultSetOverlappingRetry", DefaultParameterSetName = "Chosen")]
    public sealed class DefaultSetOverlappingRetryCommand : RetryScheduleCommand
    {
        [Parameter(ValueFromPipeline = true)]
        [Parameter(ValueFromPipeline = true, ParameterSetName = "Chosen")]
        public override PSObject Value { get { return base.Value; } set { base.Value = value; } }
    }

    [Cmdlet("Test", "OverlappingRetry")]
    public sealed class OverlappingRetryCommand : RetryScheduleCommand
    {
        [Parameter(ValueFromPipeline = true)]
        [Parameter(ValueFromPipeline = true, ParameterSetName = "Chosen")]
        public override PSObject Value { get { return base.Value; } set { base.Value = value; } }
    }

    public sealed class PrefixAttribute : ArgumentTransformationAttribute
    {
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            return new PSObject("transformed:" + inputData);
        }
    }

    [Cmdlet("Test", "TransformedPipelineRecord")]
    public sealed class TransformedInputCommand : RecordCommand
    {
        [Parameter(ValueFromPipeline = true)]
        [Prefix]
        [ValidatePattern("^transformed:")]
        public override PSObject Value { get { return base.Value; } set { base.Value = value; } }
    }

    [Cmdlet("Test", "PropertyPipelineRecord")]
    public sealed class PropertyInputCommand : RecordCommand
    {
        [Parameter(ValueFromPipeline = true)]
        public override PSObject Value { get { return base.Value; } set { base.Value = value; } }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string Name { get; set; }

        protected override void ProcessRecord() { WriteObject(Name ?? "missing"); }
    }

    [Cmdlet("Test", "TypedPipelineRecord")]
    public sealed class TypedInputCommand : RecordCommand
    {
        [Parameter(ValueFromPipeline = true)]
        [PSTypeName("Pipeline.Record")]
        public override PSObject Value { get { return base.Value; } set { base.Value = value; } }
    }
}
'@
        $bindingModule = Import-Module -Assembly $types[0].Assembly -PassThru

        function Invoke-RetrySchedule {
            param(
                [string]$Command,
                [int]$RejectedAttemptCount,
                [switch]$NormalBinding,
                [switch]$TraceBinding
            )

            # A matching default overridden by the explicit argument forces normal binding
            # without applying a default value or changing the setter's retry behavior.
            $PSDefaultParameterValues = if ($NormalBinding) { @{ "${Command}:RejectedAttemptCount" = 0 } } else { @{} }
            $state = @{ Records = @(); Errors = @(); AttemptKind = @(); TraceText = '' }
            $expression = {
                $bindingErrors = @()
                $state.Records = @('retry', 'next' | & $Command -Choose -RejectedAttemptCount $RejectedAttemptCount -ErrorAction SilentlyContinue -ErrorVariable bindingErrors)
                $state.Errors = $bindingErrors
            }

            if ($TraceBinding) {
                $tracePath = Join-Path $TestDrive "$Command-$RejectedAttemptCount-$NormalBinding-trace.txt"
                Trace-Command -Name ParameterBinding -Expression $expression -FilePath $tracePath
                $state.TraceText = Get-Content $tracePath -Raw
                $pattern = 'Parameter \[Value\] PIPELINE INPUT ValueFromPipeline (NO COERCION|WITH COERCION)'
                $state.AttemptKind = @([regex]::Matches($state.TraceText, $pattern) | ForEach-Object { $_.Groups[1].Value })
            } else {
                . $expression
            }

            $state
        }
    }

    AfterAll {
        Remove-Module $bindingModule
    }

    It "assigns each input and restores the original value between records" {
        $result = 1, 'two', $null, 4 | Test-PipelineRecord
        $result.Count | Should -Be 4
        $result[0].Value | Should -Be 1
        $result[1].Value | Should -Be 'two'
        $result[2].Value | Should -BeNullOrEmpty
        $result[3].Value | Should -Be 4
        $result[3].Assignments.Count | Should -Be 7
        $result[3].Assignments[1] | Should -Be 'initial'
        $result[3].Assignments[3] | Should -Be 'initial'
        $result[3].Assignments[5] | Should -Be 'initial'
        foreach ($record in $result) {
            $record.IsBound | Should -BeTrue
            [object]::ReferenceEquals($record.Value, $record.BoundValue) | Should -BeTrue
            $record.ParameterSet | Should -Be '__AllParameterSets'
        }
    }

    It "preserves wrapped object identity and does not enumerate collection inputs" {
        $item = [pscustomobject]@{ Name = 'record' }
        $collection = @(1, 2)
        $result = $item, $collection | Test-PipelineRecord
        [object]::ReferenceEquals($result[0].Value, $item) | Should -BeTrue
        [object]::ReferenceEquals($result[1].Value, $collection) | Should -BeTrue
    }

    It "binds object parameters without unwrapping inputs and restores their original value" {
        $item = [pscustomobject]@{ Name = 'record' }
        $collection = @(1, 2)
        $result = $item, $collection, $null, 4 | Test-ObjectPipelineRecord
        $result.Count | Should -Be 4
        [object]::ReferenceEquals($result[0].Value, $item) | Should -BeTrue
        [object]::ReferenceEquals($result[1].Value, $collection) | Should -BeTrue
        $result[2].Value | Should -BeNullOrEmpty
        $result[3].Value | Should -Be 4
        $result[3].Assignments.Count | Should -Be 7
        foreach ($index in 1, 3, 5) {
            $result[3].Assignments[$index] | Should -Be 'initial'
        }
        foreach ($record in $result) {
            $record.IsBound | Should -BeTrue
            [object]::ReferenceEquals($record.Value, $record.BoundValue) | Should -BeTrue
        }
    }

    It "constructs PSObject arrays from scalar and collection records without losing extended properties" {
        $item = [pscustomobject]@{ Name = 'record' }
        $collection = @(1, $item, $null)
        $result = 1, $item, $collection, 4 | Test-ArrayPipelineRecord
        $result.Count | Should -Be 4
        $result[0].Value.GetType() | Should -Be ([psobject[]])
        $result[0].Value.Count | Should -Be 1
        $result[0].Value[0] | Should -Be 1
        [object]::ReferenceEquals($result[1].Value[0], $item) | Should -BeTrue
        $result[2].Value.Count | Should -Be 3
        $result[2].Value[0] | Should -Be 1
        [object]::ReferenceEquals($result[2].Value[1], $item) | Should -BeTrue
        $result[2].Value[2] | Should -BeNullOrEmpty
        $result[3].Value[0] | Should -Be 4
        $result[3].Assignments.Count | Should -Be 7
        foreach ($index in 1, 3, 5) {
            [object]::ReferenceEquals($result[3].Assignments[$index], $result[3].Assignments[1]) | Should -BeTrue
            $result[3].Assignments[$index][0] | Should -Be 'initial'
        }
        foreach ($record in $result) {
            $record.IsBound | Should -BeTrue
            [object]::ReferenceEquals($record.Value, $record.BoundValue) | Should -BeTrue
        }
    }

    It "preserves an existing PSObject array and distinguishes null from empty arrays" {
        $array = [psobject[]]@(1, 2)
        $empty = [psobject[]]@()
        $result = $array, $null, $empty, 3 | Test-ArrayPipelineRecord
        $result.Count | Should -Be 4
        [object]::ReferenceEquals($result[0].Value, $array) | Should -BeTrue
        [object]::ReferenceEquals($result[1].Value, $null) | Should -BeTrue
        $result[2].Value.GetType() | Should -Be ([psobject[]])
        $result[2].Value.Count | Should -Be 0
        $result[3].Value[0] | Should -Be 3
    }

    It "enforces mandatory array null, empty, and element checks and continues with subsequent records" {
        $bindingErrors = @()
        $empty = [psobject[]]@()
        $withNull = [psobject[]]@(1, $null)
        $result = 1, $null, $empty, $withNull, 2 | Test-MandatoryArrayPipelineRecord -ErrorAction SilentlyContinue -ErrorVariable bindingErrors
        $result.Count | Should -Be 2
        $result[0].Value[0] | Should -Be 1
        $result[1].Value[0] | Should -Be 2
        $bindingErrors.Count | Should -Be 3
        $bindingErrors[0].FullyQualifiedErrorId | Should -Be 'ParameterArgumentValidationErrorNullNotAllowed,PipelineBindingTests.MandatoryArrayInputCommand'
        $bindingErrors[1].FullyQualifiedErrorId | Should -Be 'ParameterArgumentValidationErrorEmptyArrayNotAllowed,PipelineBindingTests.MandatoryArrayInputCommand'
        $bindingErrors[2].FullyQualifiedErrorId | Should -Be 'ParameterArgumentValidationErrorNullNotAllowed,PipelineBindingTests.MandatoryArrayInputCommand'
    }

    It "keeps explicit <Command> values out of pipeline candidate selection" -TestCases @(
        @{ Command = 'Test-ObjectPipelineRecord'; TypeName = 'ObjectInputCommand' }
        @{ Command = 'Test-ArrayPipelineRecord'; TypeName = 'ArrayInputCommand' }
    ) {
        param($Command, $TypeName)
        $bindingErrors = @()
        $result = 1, 2 | & $Command -Value 'explicit' -ErrorAction SilentlyContinue -ErrorVariable bindingErrors
        $result | Should -BeNullOrEmpty
        $bindingErrors.Count | Should -Be 2
        $bindingErrors[0].FullyQualifiedErrorId | Should -Be "InputObjectNotBound,PipelineBindingTests.$TypeName"
    }

    It "serializes mixed pipeline records through ConvertTo-Json" {
        $result = 1, [pscustomobject]@{ Name = 'record' }, $null, @(2, 3) | ConvertTo-Json -Compress
        $result | Should -Be '[1,{"Name":"record"},null,[2,3]]'
    }

    It "does not bind an AutomationNull sentinel as a pipeline record for <Command>" -TestCases @(
        @{ Command = 'Test-PipelineRecord' }
        @{ Command = 'Test-ObjectPipelineRecord' }
        @{ Command = 'Test-ArrayPipelineRecord' }
    ) {
        param($Command)
        $result = 1, [System.Management.Automation.Internal.AutomationNull]::Value, 2 | & $Command
        $result.Count | Should -Be 2
        $result.Value | Should -Be @(1, 2)
    }

    It "does not replace an explicitly bound value with pipeline input" {
        $bindingErrors = @()
        $result = 1, 2 | Test-PipelineRecord -Value 'explicit' -ErrorAction SilentlyContinue -ErrorVariable bindingErrors
        $result | Should -BeNullOrEmpty
        $bindingErrors.Count | Should -Be 2
        $bindingErrors[0].FullyQualifiedErrorId | Should -Be 'InputObjectNotBound,PipelineBindingTests.RecordInputCommand'
    }

    It "preserves default parameter binding for <Command>" -TestCases @(
        @{ Command = 'Test-PipelineRecord'; TypeName = 'RecordInputCommand' }
        @{ Command = 'Test-ObjectPipelineRecord'; TypeName = 'ObjectInputCommand' }
        @{ Command = 'Test-ArrayPipelineRecord'; TypeName = 'ArrayInputCommand' }
    ) {
        param($Command, $TypeName)
        $PSDefaultParameterValues = @{ "${Command}:Value" = 'default' }
        $result = & $Command
        $result.Value | Should -Be 'default'
        $result.IsBound | Should -BeTrue
        $bindingErrors = @()
        $result = 1, 2 | & $Command -ErrorAction SilentlyContinue -ErrorVariable bindingErrors
        $result | Should -BeNullOrEmpty
        $bindingErrors.Count | Should -Be 2
        $bindingErrors[0].FullyQualifiedErrorId | Should -Be "InputObjectNotBound,PipelineBindingTests.$TypeName"
    }

    It "binds pipeline records when a default has bound another parameter" {
        $PSDefaultParameterValues = @{ 'Test-PipelineRecord:Label' = 'default label' }
        $result = 1, 2 | Test-PipelineRecord
        $result.Value | Should -Be @(1, 2)
        $result.Label | Should -Be @('default label', 'default label')
        $result[1].Assignments.Count | Should -Be 3
        $result[1].Assignments[1] | Should -Be 'initial'
    }

    It "binds pipeline records when an explicit argument overrides a discovered default" {
        $PSDefaultParameterValues = @{ 'Test-PipelineRecord:Label' = 'default label' }
        $result = 1, 2 | Test-PipelineRecord -Label 'explicit label'
        $result.Value | Should -Be @(1, 2)
        $result.Label | Should -Be @('explicit label', 'explicit label')
    }

    It "retries script-block defaults after pipeline binding without replacing the input" {
        $PSDefaultParameterValues = @{
            'Test-PipelineRecord:Label' = {
                param($BindingState)
                if ($BindingState.BoundParameters.Contains('Value')) {
                    'late label'
                }
            }
        }
        $result = 1, 2 | Test-PipelineRecord
        $result.Value | Should -Be @(1, 2)
        $result.Label | Should -Be @('late label', 'late label')
        $result[1].Assignments.Count | Should -Be 3
    }

    It "retains default-related error elaboration for mandatory pipeline parameters" {
        $PSDefaultParameterValues = @{ 'Test-MandatoryPipelineRecord:Label' = 'default label' }
        $bindingErrors = @()
        $result = 1, $null, 2 | Test-MandatoryPipelineRecord -ErrorAction SilentlyContinue -ErrorVariable bindingErrors
        $result.Value | Should -Be @(1, 2)
        $bindingErrors.Count | Should -Be 1
        $bindingErrors[0].Exception.Message | Should -Match 'default'
    }

    It "retains delayed script-block argument binding for <Command>" -TestCases @(
        @{ Command = 'Test-PipelineRecord' }
        @{ Command = 'Test-ArrayPipelineRecord' }
    ) {
        param($Command)
        $result = 1, 2 | & $Command -Value { $_ * 2 }
        $result.Value | Should -Be @(2, 4)
    }

    It "retries with coercion when a PSObject setter rejects the first binding attempt" {
        $result = 1, 'retry', 3 | Test-RetryPipelineRecord
        $result | Should -Be @(0, 2, 2)
    }

    It "preserves <Command> retry order when <RejectedAttemptCount> setter attempts fail" -TestCases @(
        @{ Command = 'Test-DefaultSetRetry'; RejectedAttemptCount = 2; AttemptsPerPhase = 2 }
        @{ Command = 'Test-DefaultSetOverlappingRetry'; RejectedAttemptCount = 2; AttemptsPerPhase = 3 }
        @{ Command = 'Test-OverlappingRetry'; RejectedAttemptCount = 2; AttemptsPerPhase = 2 }
        @{ Command = 'Test-DefaultSetRetry'; RejectedAttemptCount = 3; AttemptsPerPhase = 2 }
        @{ Command = 'Test-DefaultSetOverlappingRetry'; RejectedAttemptCount = 5; AttemptsPerPhase = 3 }
        @{ Command = 'Test-OverlappingRetry'; RejectedAttemptCount = 3; AttemptsPerPhase = 2 }
    ) {
        param($Command, $RejectedAttemptCount, $AttemptsPerPhase)

        $normal = Invoke-RetrySchedule -Command $Command -RejectedAttemptCount $RejectedAttemptCount -NormalBinding -TraceBinding
        $fast = Invoke-RetrySchedule -Command $Command -RejectedAttemptCount $RejectedAttemptCount -TraceBinding
        $normal.TraceText | Should -Not -Match 'Using fast-path pipeline input binding'
        $fast.TraceText | Should -Match 'Using fast-path pipeline input binding'
        $fast.Errors.Count | Should -Be 0
        $normal.Errors.Count | Should -Be 0
        $fast.Records.Value | Should -Be @('retry', 'next')
        $fast.Records[0].Attempts | Should -Be ($RejectedAttemptCount + 1)
        $fast.Records[1].Attempts | Should -Be ($RejectedAttemptCount + 1)
        ($fast.Records | ConvertTo-Json -Depth 4 -Compress) | Should -Be ($normal.Records | ConvertTo-Json -Depth 4 -Compress)
        foreach ($record in $fast.Records) {
            $record.IsBound | Should -BeTrue
            $record.ParameterSet | Should -Be 'Chosen'
            [object]::ReferenceEquals($record.Value, $record.BoundValue) | Should -BeTrue
        }

        $expectedKind = @('NO COERCION') * $AttemptsPerPhase
        $expectedKind += @('WITH COERCION') * ($RejectedAttemptCount + 1 - $AttemptsPerPhase)
        $expectedKind += 'NO COERCION'
        $normal.AttemptKind | Should -Be $expectedKind
        $fast.AttemptKind | Should -Be $expectedKind
    }

    It "preserves <Command> exhausted retries and continues with the next record" -TestCases @(
        @{ Command = 'Test-DefaultSetRetry'; TypeName = 'DefaultSetRetryCommand'; AttemptsPerPhase = 2 }
        @{ Command = 'Test-DefaultSetOverlappingRetry'; TypeName = 'DefaultSetOverlappingRetryCommand'; AttemptsPerPhase = 3 }
        @{ Command = 'Test-OverlappingRetry'; TypeName = 'OverlappingRetryCommand'; AttemptsPerPhase = 2 }
    ) {
        param($Command, $TypeName, $AttemptsPerPhase)

        $normal = Invoke-RetrySchedule -Command $Command -RejectedAttemptCount 100 -NormalBinding -TraceBinding
        $fast = Invoke-RetrySchedule -Command $Command -RejectedAttemptCount 100 -TraceBinding
        $normal.TraceText | Should -Not -Match 'Using fast-path pipeline input binding'
        $fast.TraceText | Should -Match 'Using fast-path pipeline input binding'
        $fast.Errors.Count | Should -Be 1
        $normal.Errors.Count | Should -Be 1
        $fast.Errors[0].FullyQualifiedErrorId | Should -Be "InputObjectNotBound,PipelineBindingTests.$TypeName"
        $fast.Errors[0].FullyQualifiedErrorId | Should -Be $normal.Errors[0].FullyQualifiedErrorId
        $fast.Errors[0].TargetObject | Should -Be 'retry'
        $fast.Records.Count | Should -Be 1
        $fast.Records[0].Value | Should -Be 'next'
        $fast.Records[0].Attempts | Should -Be (2 * $AttemptsPerPhase)
        ($fast.Records | ConvertTo-Json -Depth 4 -Compress) | Should -Be ($normal.Records | ConvertTo-Json -Depth 4 -Compress)
        $expectedKind = @('NO COERCION') * $AttemptsPerPhase + @('WITH COERCION') * $AttemptsPerPhase + @('NO COERCION')
        $normal.AttemptKind | Should -Be $expectedKind
        $fast.AttemptKind | Should -Be $expectedKind
    }

    It "keeps fast-path binding enabled when <TraceSource> tracing is enabled" -TestCases @(
        @{ TraceSource = 'ParameterBinding' }
        @{ TraceSource = 'ParameterBinderController' }
    ) {
        param($TraceSource)

        $tracePath = Join-Path $TestDrive "$TraceSource-fast-path.txt"
        $untraced = 1, 2 | Test-PipelineRecord
        $traced = Trace-Command -Name $TraceSource -Expression { 1, 2 | Test-PipelineRecord } -FilePath $tracePath
        $traced.Value | Should -Be @(1, 2)
        ($traced | ConvertTo-Json -Depth 4 -Compress) | Should -Be ($untraced | ConvertTo-Json -Depth 4 -Compress)
        $traceText = Get-Content $tracePath -Raw
        $traceText | Should -Match 'Using fast-path pipeline input binding for parameter \[Value\]'
        if ($TraceSource -eq 'ParameterBinding') {
            $traceText | Should -Match 'PIPELINE INPUT ValueFromPipeline NO COERCION'
        }
    }

    It "does not identify normal binding as fast-path binding in traces" {
        $PSDefaultParameterValues = @{ 'Test-PipelineRecord:Label' = 'default label' }
        $tracePath = Join-Path $TestDrive 'normal-pipeline-binding.txt'
        $result = Trace-Command -Name ParameterBinding, ParameterBinderController -Expression { 1, 2 | Test-PipelineRecord } -FilePath $tracePath
        $result.Value | Should -Be @(1, 2)
        $result.Label | Should -Be @('default label', 'default label')
        $traceText = Get-Content $tracePath -Raw
        $traceText | Should -Not -Match 'Using fast-path pipeline input binding'
        $traceText | Should -Match 'PIPELINE INPUT ValueFromPipeline NO COERCION'
    }

    It "reports mandatory null errors and continues with subsequent records" {
        $bindingErrors = @()
        $result = 1, $null, 2 | Test-MandatoryPipelineRecord -ErrorAction SilentlyContinue -ErrorVariable bindingErrors
        $result.Value | Should -Be @(1, 2)
        $bindingErrors.Count | Should -Be 1
        $bindingErrors[0].FullyQualifiedErrorId | Should -Be 'ParameterArgumentValidationErrorNullNotAllowed,PipelineBindingTests.MandatoryInputCommand'
    }

    It "retains transformations and validation on the general path" {
        $result = 1, 2 | Test-TransformedPipelineRecord
        $result.Value | Should -Be @('transformed:1', 'transformed:2')
    }

    It "binds competing property-name parameters and restores their defaults" {
        $result = [pscustomobject]@{ Name = 'first' }, [pscustomobject]@{ Other = 'second' } | Test-PropertyPipelineRecord
        $result | Should -Be @('first', 'missing')
    }

    It "continues enforcing PSTypeName constraints" {
        $item = [pscustomobject]@{ PSTypeName = 'Pipeline.Record'; Name = 'typed' }
        $bindingErrors = @()
        $result = $item, 1 | Test-TypedPipelineRecord -ErrorAction SilentlyContinue -ErrorVariable bindingErrors
        $result.Value | Should -Be $item
        $bindingErrors.Count | Should -Be 1
        $bindingErrors[0].FullyQualifiedErrorId | Should -Be 'InputObjectNotBound,PipelineBindingTests.TypedInputCommand'
    }

    It "keeps ForEach-Object callback scope and invocation behavior" {
        $total = 0
        $result = 1, 2, 3 | ForEach-Object -Begin { $total = 10 } -Process { $total += $_ } -End { $total }
        $result | Should -Be 16
        $total | Should -Be 16
        $filtered = 1, 2, 3 | Where-Object { $_ -gt 1 }
        $filtered | Should -Be @(2, 3)
    }
}

Describe "Parameter Binding Tests" -Tags "CI" {
    It "Should throw a parameter binding exception when two parameters have the same position" {
        function test-PositionalBinding1 {
            [CmdletBinding()]
            param (
            [Parameter(Position = 0)] [int]$Parameter1 = 0,
            [Parameter(Position = 0)] [int]$Parameter2 = 0
            )

            Process {
                return $true
            }
        }

        { test-PositionalBinding1 1 } | Should -Throw -ErrorId "AmbiguousPositionalParameterNoName,test-PositionalBinding1"
    }

    It "a mandatory parameter can't be passed a null if it doesn't have AllowNullAttribute" {
        function test-allownullattributes {
            [CmdletBinding()]
            param (
            [string]$Parameter1 = "default1",
            [Parameter(Mandatory = $true)] [string]$Parameter2 = "default2",
            [Parameter(Mandatory = $true)] [string]$Parameter3 = "default3",
            [AllowNull()] [int]$Parameter4 = 0,
            [AllowEmptyString()][int]$Parameter5 = 0,
            [Parameter(Mandatory = $true)] [int]$ShowMe = 0
            )

            Process {
                switch ( $ShowMe )
                {
                    1 {
                        return $Parameter1
                        break
                        }
                    2 {
                        return $Parameter2
                        break
                        }
                    3 {
                        return $Parameter3
                        break
                        }
                    4 {
                        return $Parameter4
                        break
                        }
                    5 {
                        return $Parameter5
                        break
                        }
                }
            }
        }

        $e = { test-allownullattributes -Parameter2 1 -Parameter3 $null -ShowMe 1 } |
            Should -Throw -ErrorId "ParameterArgumentValidationErrorEmptyStringNotAllowed,test-allownullattributes" -PassThru
        $e.CategoryInfo | Should -Match "ParameterBindingValidationException"
        $e.Exception.Message | Should -Match "Parameter3"
    }

    It "can't pass an argument that looks like a boolean parameter to a named string parameter" {
        function test-namedwithboolishargument {
            [CmdletBinding()]
            param (
            [bool] $Parameter1 = $false,
            [Parameter(Position = 0)] [string]$Parameter2 = ""
            )

            Process {
                return $Parameter2
            }
        }

        $e = { test-namedwithboolishargument -Parameter2 -Parameter1 } | Should -Throw -ErrorId "MissingArgument,test-namedwithboolishargument" -PassThru
        $e.CategoryInfo | Should -Match "ParameterBindingException"
        $e.Exception.Message | Should -Match "Parameter2"
    }

    It "Verify that a SwitchParameter's IsPresent member is false if the parameter is not specified" {
        function test-singleswitchparameter {
            [CmdletBinding()]
            param (
            [switch]$Parameter1
            )

            Process {
                return $Parameter1.IsPresent
            }
        }

        $result = test-singleswitchparameter
        $result | Should -BeFalse
    }

    It "Verify that a bool parameter returns proper value" {
        function test-singleboolparameter {
            [CmdletBinding()]
            param (
            [bool]$Parameter1 = $false
            )

            Process {
                return $Parameter1
            }
        }

        $result1 = test-singleboolparameter
        $result1 | Should -BeFalse

        $result2 = test-singleboolparameter -Parameter1:1
        $result2 | Should -BeTrue
    }

    It "Should throw a exception when passing a string that can't be parsed by Int" {
        function test-singleintparameter {
            [CmdletBinding()]
            param (
            [int]$Parameter1 = 0
            )

            Process {
                return $Parameter1
            }
        }

        $e = { test-singleintparameter -Parameter1 'exampleInvalidParam' } |
            Should -Throw -ErrorId "ParameterArgumentTransformationError,test-singleintparameter" -PassThru
        $e.CategoryInfo | Should -Match "ParameterBindingArgumentTransformationException"
        $e.Exception.Message | Should -Match "The input string 'exampleInvalidParam' was not in a correct format"
        $e.Exception.Message | Should -Match "Parameter1"
    }

    It "Verify that WhatIf is available when SupportShouldProcess is true" {
        function test-supportsshouldprocess2 {
            [CmdletBinding(SupportsShouldProcess = $true)]
            Param ()

            Process {
                return 1
            }
        }

        $result = test-supportsshouldprocess2 -Whatif
        $result | Should -Be 1
    }

    It "Verify that ValueFromPipeline takes precedence over ValueFromPipelineByPropertyName without type coercion" {
        function test-bindingorder2 {
            [CmdletBinding()]
            param (
            [Parameter(ValueFromPipeline = $true, ParameterSetName = "one")] [string]$Parameter1 = "",
            [Parameter(ValueFromPipelineByPropertyName = $true, ParameterSetName = "two")] [int]$Length = 0
            )

            Process {
                return "$Parameter1 - $Length"
            }
        }

        $result = '0123' | test-bindingorder2
        $result | Should -Be "0123 - 0"
    }

    It "Verify that a ScriptBlock object can be delay-bound to a parameter of type FileInfo with pipeline input" {
        function test-scriptblockbindingfrompipeline {
            [CmdletBinding()]
            param (
            [Parameter(ValueFromPipeline = $true)] [System.IO.FileInfo]$Parameter1
            )

            Process {
                return $Parameter1.Name
            }
        }
        $testFile = Join-Path $TestDrive -ChildPath "testfile.txt"
        New-Item -Path $testFile -ItemType file -Force
        $result = Get-Item $testFile | test-scriptblockbindingfrompipeline -Parameter1 {$_}
        $result | Should -Be "testfile.txt"
    }

    It "Verify that a dynamic parameter named WhatIf doesn't conflict if SupportsShouldProcess is false" {
        function test-dynamicparameters3 {
            [CmdletBinding(SupportsShouldProcess = $false)]
            param (
            [Parameter(ParameterSetName = "one")] [int]$Parameter1 = 0,
            [Parameter(ParameterSetName = "two")] [int]$Parameter2 = 0,
            [int]$WhatIf = 0
            )
        }

        { test-dynamicparameters3 -Parameter1 1 } | Should -Not -Throw
    }

    It "Verify that an int can be bound to a parameter of type Array" {
        function test-collectionbinding1 {
            [CmdletBinding()]
            param (
            [array]$Parameter1,
            [int[]]$Parameter2
            )

            Process {
                $result = ""
                if($null -ne $Parameter1)
                {
                    $result += " P1"
                    foreach ($object in $Parameter1)
                    {
                        $result = $result + ":" + $object.GetType().Name + "," + $object
                    }
                }
                if($null -ne $Parameter2)
                {
                    $result += " P2"
                    foreach ($object in $Parameter2)
                    {
                        $result = $result + ":" + $object.GetType().Name + "," + $object
                    }
                }
                return $result.Trim()
            }
        }

        $result = test-collectionbinding1 -Parameter1 1 -Parameter2 2
        $result | Should -Be "P1:Int32,1 P2:Int32,2"
    }

    It "Verify that a dynamic parameter and an alias can't have the same name" {
        function test-nameconflicts6 {
            [CmdletBinding()]
            param (
            [Alias("Parameter2")]
            [int]$Parameter1 = 0,
            [int]$Parameter2 = 0
            )
        }

        $e = { test-nameconflicts6 -Parameter2 1 } | Should -Throw -ErrorId "ParameterNameConflictsWithAlias" -PassThru
        $e.CategoryInfo | Should -Match "MetadataException"
        $e.Exception.Message | Should -Match "Parameter1"
        $e.Exception.Message | Should -Match "Parameter2"
    }

    It "PipelineVariable shouldn't cause a NullRef exception when 'DynamicParam' block is present" {
        function DynamicParamTest {
            [CmdletBinding()]
            param()
            dynamicparam { }
            process { 'hi' }
        }

        DynamicParamTest -PipelineVariable bar | ForEach-Object { $bar } | Should -Be "hi"
    }

    It 'Dynamic parameter is found even if globbed path does not exist' {
        $guid = New-Guid

        # This test verifies that the ErrorRecord is coming from parameter validation on a dynamic parameter
        # instead of an error indicating that the dynamic parameter is not found
        { Copy-Item "~\$guid*" -Destination ~ -ToSession $null } | Should -Throw -ErrorId 'ParameterArgumentValidationError'
    }

    It 'PipelineVariable should not cause variable-removal exception (issue #16155)' {
        function Invoke-AddOne {
            param (
                [Parameter(ValueFromPipeline)]
                [int]$Number
            )

            Begin {
                $testValue = 'prefix-'
            }

            Process {
                $testValue + $Number
            }
        }

        1,2,3 | Invoke-AddOne -PipelineVariable testValue | ForEach-Object { $testValue } | Should -Be @('prefix-1', 'prefix-2', 'prefix-3')
        { Get-Variable -Name testValue -ErrorAction Stop } | Should -Throw -ErrorId 'VariableNotFound,Microsoft.PowerShell.Commands.GetVariableCommand'

        $results = & { $test = 'str'; 1,2,3 | Invoke-AddOne -PipelineVariable test | ForEach-Object { $test }; Get-Variable test }
        $results | Should -HaveCount 4
        $results[0] | Should -BeExactly 'prefix-1'
        $results[1] | Should -BeExactly 'prefix-2'
        $results[2] | Should -BeExactly 'prefix-3'
        $results[3] -is [psvariable] | Should -BeTrue

        $results = & { Set-Variable -Name test -Value 'str'; 1,2,3 | Invoke-AddOne -PipelineVariable test | ForEach-Object { $test }; Get-Variable test }
        $results | Should -HaveCount 4
        $results[0] | Should -BeExactly 'prefix-1'
        $results[1] | Should -BeExactly 'prefix-2'
        $results[2] | Should -BeExactly 'prefix-3'
        $results[3] -is [psvariable] | Should -BeTrue
    }

    Context "PipelineVariable Behaviour" {

        BeforeAll {
            function Write-PipelineVariable {
                [CmdletBinding()]
                [OutputType([int])]
                param(
                    [Parameter(ValueFromPipeline)]
                    $a
                )
                begin { 1 }
                process { 2 }
                end { 3 }
            }

            $testScripts = @(
                @{
                    CmdletType = 'Script Cmdlet'
                    Script     = {
                        1..3 |
                            Write-PipelineVariable -PipelineVariable pipe |
                            Select-Object -Property @(
                                @{ Name = "PipelineVariableSet"; Expression = { $null -ne $pipe ? $true : $false } }
                                @{ Name = "PipelineVariable"; Expression = { $pipe } }
                            )
                    }
                }
                @{
                    CmdletType = 'Compiled Cmdlet'
                    Script     = {
                        1..3 |
                            Write-PipelineVariable |
                            ForEach-Object { $_ } -PipelineVariable pipe |
                            Select-Object -Property @(
                                @{ Name = "PipelineVariableSet"; Expression = { $null -ne $pipe ? $true : $false } }
                                @{ Name = "PipelineVariable"; Expression = { $pipe } }
                            )
                    }
                }
            )
        }

        AfterAll {
            Remove-Item -Path 'function:Write-PipelineVariable'
        }

        It 'should set the pipeline variable every time for a <CmdletType>' -TestCases $testScripts {
            param($Script, $CmdletType)

            $result = & $Script
            $result.Count | Should -Be 5
            $result.PipelineVariableSet | Should -Not -Contain $false
            $result.PipelineVariable | Should -Be 1, 2, 2, 2, 3
        }
    }

    Context "Use automatic variables as default value for parameters" {
        BeforeAll {
            ## Explicit use of 'CmdletBinding' make it a script cmdlet
            $test1 = @'
                [CmdletBinding()]
                param ($Root = $PSScriptRoot)
                "[$Root]"
'@
            ## Use of 'Parameter' implicitly make it a script cmdlet
            $test2 = @'
                param (
                    [Parameter()]
                    $Root = $PSScriptRoot
                )
                "[$Root]"
'@
            $tempDir = Join-Path -Path $TestDrive -ChildPath "DefaultValueTest"
            $test1File = Join-Path -Path $tempDir -ChildPath "test1.ps1"
            $test2File = Join-Path -Path $tempDir -ChildPath "test2.ps1"

            $expected = "[$tempDir]"
            $pwsh = "$PSHOME\pwsh"

            $null = New-Item -Path $tempDir -ItemType Directory -Force
            Set-Content -Path $test1File -Value $test1 -Force
            Set-Content -Path $test2File -Value $test2 -Force
        }

        AfterAll {
            Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
        }

        It "Test dot-source should evaluate '`$PSScriptRoot' for parameter default value" {
            $result = . $test1File
            $result | Should -Be $expected

            $result = . $test2File
            $result | Should -Be $expected
        }

        It "Test 'powershell -File' should evaluate '`$PSScriptRoot' for parameter default value" {
            $result = & $pwsh -NoProfile -File $test1File
            $result | Should -Be $expected

            $result = & $pwsh -NoProfile -File $test2File
            $result | Should -Be $expected
        }
    }

    Context "ValueFromRemainingArguments" {
        BeforeAll {
            function Test-BindingFunction {
                param (
                    [Parameter(ValueFromRemainingArguments)]
                    [object[]] $Parameter
                )

                return [pscustomobject] @{
                    ArgumentCount = $Parameter.Count
                    Value = $Parameter
                }
            }

            # Deliberately not using TestDrive:\ here because Pester will fail to clean it up due to the
            # assembly being loaded in our process.

            if ($IsWindows)
            {
                $tempDir = $env:temp
            }
            else
            {
                $tempDir = '/tmp'
            }

            $dllPath = Join-Path $tempDir TestBindingCmdlet.dll

            $typeDefinition = '
                using System;
                using System.Management.Automation;

                [Cmdlet("Test", "BindingCmdlet")]
                public class TestBindingCommand : PSCmdlet
                {
                    [Parameter(Position = 0, ValueFromRemainingArguments = true)]
                    public string[] Parameter { get; set; }

                    protected override void ProcessRecord()
                    {
                        PSObject obj = new PSObject();

                        obj.Properties.Add(new PSNoteProperty("ArgumentCount", Parameter.Length));
                        obj.Properties.Add(new PSNoteProperty("Value", Parameter));

                        WriteObject(obj);
                    }
                }
            '
            if ( !(Test-Path $dllPath))
            {
                Add-Type -OutputAssembly $dllPath -TypeDefinition $typeDefinition
            }

            Import-Module $dllPath
        }

        AfterAll {
            Get-Module TestBindingCmdlet | Remove-Module -Force
        }

        It "Binds properly when passing an explicit array to an advanced function" {
            $result = Test-BindingFunction 1,2,3

            $result.ArgumentCount | Should -Be 3
            $result.Value[0] | Should -Be 1
            $result.Value[1] | Should -Be 2
            $result.Value[2] | Should -Be 3
        }

        It "Binds properly when passing multiple arguments to an advanced function" {
            $result = Test-BindingFunction 1 2 3

            $result.ArgumentCount | Should -Be 3
            $result.Value[0] | Should -Be 1
            $result.Value[1] | Should -Be 2
            $result.Value[2] | Should -Be 3
        }

        It "Binds properly when passing an explicit array to a cmdlet" {
            $result = Test-BindingCmdlet 1,2,3

            $result.ArgumentCount | Should -Be 3
            $result.Value[0] | Should -Be 1
            $result.Value[1] | Should -Be 2
            $result.Value[2] | Should -Be 3
        }

        It "Binds properly when passing multiple arguments to a cmdlet" {
            $result = Test-BindingCmdlet 1 2 3

            $result.ArgumentCount | Should -Be 3
            $result.Value[0] | Should -Be 1
            $result.Value[1] | Should -Be 2
            $result.Value[2] | Should -Be 3
        }

        It "Binds properly when collections of type other than object[] are used on an advanced function" {
            $list = [Collections.Generic.List[int]](1..3)
            $result = Test-BindingFunction $list

            $result.ArgumentCount | Should -Be 3
            $result.Value[0] | Should -Be 1
            $result.Value[1] | Should -Be 2
            $result.Value[2] | Should -Be 3
        }

        It "Binds properly when collections of type other than object[] are used on a cmdlet" {
            $list = [Collections.Generic.List[int]](1..3)
            $result = Test-BindingCmdlet $list

            $result.ArgumentCount | Should -Be 3
            $result.Value[0] | Should -Be 1
            $result.Value[1] | Should -Be 2
            $result.Value[2] | Should -Be 3
        }
    }
}
