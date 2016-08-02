# Tests related to TFS item 1370133 [PSUpgrade] Need -NoNewline parameter on Out-File, Add-Content and Set-Content
# Connect request https://connect.microsoft.com/PowerShell/feedback/details/524739/need-nonewline-parameter-on-out-file-add-content-and-set-content

Describe "Tests for -NoNewline parameter of Out-File, Add-Content and Set-Content" -tags "Feature" {
    
    It "NoNewline parameter works on Out-File" {
         $temp = New-TemporaryFile
         1..5 | Out-File $temp.FullName -Encoding 'ASCII' -NoNewline
         (Get-Content $temp -Encoding Byte).Count | Should Be 5
         Remove-Item $temp -ErrorAction SilentlyContinue -Force
    }

    It "NoNewline parameter works on Set-Content" {
         $temp = New-TemporaryFile
         Set-Content -Path $temp.FullName -Value 'a','b','c' -Encoding 'ASCII' -NoNewline
         (Get-Content $temp -Encoding Byte).Count | Should Be 3
         Remove-Item $temp -ErrorAction SilentlyContinue -Force
    }

    It "NoNewline parameter works on Add-Content" {
         $temp = New-TemporaryFile
         1..9 | %{Add-Content -Path $temp.FullName -Value $_ -Encoding 'ASCII' -NoNewline}
         (Get-Content $temp -Encoding Byte).Count | Should Be 9
         Remove-Item $temp -ErrorAction SilentlyContinue -Force
    }
}
