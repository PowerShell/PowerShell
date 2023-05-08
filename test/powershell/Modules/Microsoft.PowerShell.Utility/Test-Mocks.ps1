# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Function GetFileMock () {
    $objs = @( [pscustomobject]@{ Size=4533816; Mode="-a---l"; LastWriteTime="9/1/2015  11:15 PM"; Name="explorer.exe" },
	       [pscustomobject]@{ Size=994816;  Mode="-a---l"; LastWriteTime="9/1/2015  11:13 PM"; Name="HelpPane.exe" },
	       [pscustomobject]@{ Size=316640;  Mode="-a---l"; LastWriteTime="9/1/2015  11:17 PM"; Name="WMSysPr9.prx" },
	       [pscustomobject]@{ Size=215040;  Mode="-a---l"; LastWriteTime="9/1/2015  11:20 PM"; Name="notepad.exe"  },
	       [pscustomobject]@{ Size=207239;  Mode="-a----"; LastWriteTime="10/7/2015  2:37 PM"; Name="setupact.log" },
	       [pscustomobject]@{ Size=181064;  Mode="-a----"; LastWriteTime="9/9/2015  11:54 PM"; Name="PSEXESVC.EXE" })
    return $objs
}

filter addOneToSizeProperty() {
    $_.Size += 1
    $_
}

filter pipelineConsume() {
    $_
}
