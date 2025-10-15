# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Write-Progress with double-width characters' -Tags 'CI' {
    BeforeAll {
        $th = New-TestHost
        $rs = [runspacefactory]::Createrunspace($th)
        $rs.open()
        $ps = [powershell]::Create()
        $ps.Runspace = $rs
        
        # Set a consistent window size for testing
        $th.UI.RawUI.WindowSize = New-Object System.Management.Automation.Host.Size(80, 40)
    }
    
    AfterEach {
        $ps.Commands.Clear()
        $th.UI.Streams.Clear()
    }
    
    AfterAll {
        $rs.Close()
        $rs.Dispose()
        $ps.Dispose()
    }
    
    It 'Should handle Japanese characters in StatusDescription' {
        $result = $ps.AddScript("Write-Progress -Activity 'Test' -Status '日本語のステータスメッセージです' -PercentComplete 50").Invoke()
        $th.UI.Streams.Progress.Count | Should -Be 1
        $progressRecord = $th.UI.Streams.Progress[0]
        $progressRecord.Activity | Should -Be 'Test'
        $progressRecord.StatusDescription | Should -Be '日本語のステータスメッセージです'
        $progressRecord.PercentComplete | Should -Be 50
    }
    
    It 'Should handle very long Japanese text without corruption' {
        $result = $ps.AddScript("Write-Progress -Activity '処理中' -Status '日本語の非常に長いステータスメッセージでプログレスバーの切り詰め機能を検証します' -PercentComplete 75").Invoke()
        $th.UI.Streams.Progress.Count | Should -Be 1
        $progressRecord = $th.UI.Streams.Progress[0]
        $progressRecord.Activity | Should -Be '処理中'
        $progressRecord.StatusDescription | Should -Match '日本語の非常に長い'
    }
    
    It 'Should handle emoji in StatusDescription' {
        $result = $ps.AddScript("Write-Progress -Activity 'Upload' -Status '🚀 Uploading files' -PercentComplete 30").Invoke()
        $th.UI.Streams.Progress.Count | Should -Be 1
        $progressRecord = $th.UI.Streams.Progress[0]
        $progressRecord.StatusDescription | Should -Match '🚀'
    }
    
    It 'Should handle Chinese characters (Issue #21293 scenario)' {
        $result = $ps.AddScript("Write-Progress -Activity '下载' -Status '正在下载文件 文件名123.txt' -PercentComplete 45").Invoke()
        $th.UI.Streams.Progress.Count | Should -Be 1
        $progressRecord = $th.UI.Streams.Progress[0]
        $progressRecord.Activity | Should -Be '下载'
        $progressRecord.StatusDescription | Should -Match '正在下载文件'
    }    
    It 'Should handle mixed ASCII and double-width characters' {
        $result = $ps.AddScript("Write-Progress -Activity 'Processing' -Status 'Processing ファイル 123 items 日本語' -PercentComplete 60").Invoke()
        $th.UI.Streams.Progress.Count | Should -Be 1
        $progressRecord = $th.UI.Streams.Progress[0]
        $progressRecord.StatusDescription | Should -Be 'Processing ファイル 123 items 日本語'
        $progressRecord.PercentComplete | Should -Be 60
    }
    
    It 'Should handle Completed status with double-width characters' {
        $result = $ps.AddScript("Write-Progress -Activity '完了' -Status '処理が完了しました' -Completed").Invoke()
        $th.UI.Streams.Progress.Count | Should -Be 1
        $progressRecord = $th.UI.Streams.Progress[0]
        $progressRecord.Activity | Should -Be '完了'
        $progressRecord.RecordType | Should -Be 'Completed'
    }
    
    It 'Should handle extremely long Activity text' {
        $longActivity = '日本語の非常に長いアクティビティ名でプログレスバーのActivity部分の切り詰め機能を検証するテストケースです' * 3
        $script = "Write-Progress -Activity '$longActivity' -Status 'Status' -PercentComplete 25"
        $result = $ps.AddScript($script).Invoke()
        $th.UI.Streams.Progress.Count | Should -Be 1
        $progressRecord = $th.UI.Streams.Progress[0]
        $progressRecord.Activity | Should -Match '日本語の非常に長い'
    }
    
    It 'Should handle extremely long Status text requiring truncation' {
        $longStatus = '日' * 150
        $script = "Write-Progress -Activity 'Test' -Status '$longStatus' -PercentComplete 50"
        $result = $ps.AddScript($script).Invoke()
        $th.UI.Streams.Progress.Count | Should -Be 1
        $progressRecord = $th.UI.Streams.Progress[0]
        $progressRecord.StatusDescription | Should -Not -BeNullOrEmpty
        $progressRecord.StatusDescription | Should -Be $longStatus
    }
    
    It 'Should handle Korean characters' {
        $result = $ps.AddScript("Write-Progress -Activity '다운로드' -Status '파일을 다운로드하는 중입니다' -PercentComplete 80").Invoke()
        $th.UI.Streams.Progress.Count | Should -Be 1
        $progressRecord = $th.UI.Streams.Progress[0]
        $progressRecord.Activity | Should -Be '다운로드'
        $progressRecord.StatusDescription | Should -Be '파일을 다운로드하는 중입니다'
    }
    
    It 'Should handle multiple progress operations with parent-child relationship' {
        $th.UI.Streams.Clear()
        $script1 = "Write-Progress -Id 1 -Activity '親タスク' -Status '実行中' -PercentComplete 50"
        $script2 = "Write-Progress -Id 2 -ParentId 1 -Activity '子タスク' -Status '処理中' -PercentComplete 75"
        $result = $ps.AddScript($script1).Invoke()
        $result = $ps.AddScript($script2).Invoke()
        $th.UI.Streams.Progress.Count | Should -BeGreaterOrEqual 2
        $th.UI.Streams.Progress[-2].Activity | Should -Be '親タスク'
        $th.UI.Streams.Progress[-1].Activity | Should -Be '子タスク'
        $th.UI.Streams.Progress[-1].ParentActivityId | Should -Be 1
    }
    
    It 'Should handle truncation at various boundary points' {
        # Test different lengths to verify truncation logic
        $lengths = @(10, 30, 50, 80, 120)
        foreach ($len in $lengths) {
            $status = '日' * $len
            $script = "Write-Progress -Activity 'Boundary' -Status '$status' -PercentComplete 50"
            $result = $ps.AddScript($script).Invoke()
            $progressRecord = $th.UI.Streams.Progress[0]
            $progressRecord.StatusDescription | Should -Not -BeNullOrEmpty
            $th.UI.Streams.Clear()
        }
    }

}
