# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "WebResponseObject text and binary visual views" -Tags "CI" {
    It "Decodes <ContentType> using <EncodingName>" -TestCases @(
        @{ ContentType = 'text/plain; charset=utf-8'; EncodingName = 'utf-8' }
        @{ ContentType = 'text/plain'; EncodingName = 'utf-8' }
        @{ ContentType = 'text/plain; charset=iso-8859-1'; EncodingName = 'iso-8859-1' }
        @{ ContentType = 'text/plain; charset=invalid'; EncodingName = 'utf-8' }
        @{ ContentType = 'application/json'; EncodingName = 'utf-8' }
        @{ ContentType = 'application/xml'; EncodingName = 'utf-8' }
    ) {
        param($ContentType, $EncodingName)
        $message = [System.Net.Http.HttpResponseMessage]::new([System.Net.HttpStatusCode]::OK)
        try {
            $bytes = [System.Text.Encoding]::GetEncoding($EncodingName).GetBytes('café')
            $message.Content = [System.Net.Http.ByteArrayContent]::new($bytes)
            $message.Content.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse($ContentType)
            $response = [Microsoft.PowerShell.Commands.WebResponseObject]::new($message, [System.TimeSpan]::FromSeconds(10), [System.Threading.CancellationToken]::None)
            $response.ToString() | Should -BeExactly 'café'
            $response.RawContent | Should -Match "café$"
        } finally {
            $message.Dispose()
        }
    }

    It "Preserves the ASCII visual view for binary content <ContentType>" -TestCases @(
        @{ ContentType = 'application/octet-stream' }
        @{ ContentType = 'application/octet-stream; charset=utf-8' }
        @{ ContentType = 'image/png' }
        @{ ContentType = $null }
    ) {
        param($ContentType)
        $message = [System.Net.Http.HttpResponseMessage]::new([System.Net.HttpStatusCode]::OK)
        try {
            # UTF-8 bytes must remain two ASCII replacements, not one decoded character.
            $message.Content = [System.Net.Http.ByteArrayContent]::new([byte[]](0x41, 0xc3, 0xa9, 0x00, 0x0a))
            if ($ContentType) {
                $message.Content.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse($ContentType)
            }
            $response = [Microsoft.PowerShell.Commands.WebResponseObject]::new($message, [System.TimeSpan]::FromSeconds(10), [System.Threading.CancellationToken]::None)
            $response.ToString() | Should -BeExactly "A??.`n"
            $response.RawContent.EndsWith("A??.`n") | Should -BeTrue
            $response.Content.Length | Should -Be 5
        } finally {
            $message.Dispose()
        }
    }

    It "Preserves empty bodies and filters nonprintable text characters" -TestCases @(
        @{ NonPrintable = $false; Expected = '' }
        @{ NonPrintable = $true; Expected = "a.b`nc" }
    ) {
        param($NonPrintable, $Expected)
        $body = if ($NonPrintable) { "a`0b`nc" } else { '' }
        $message = [System.Net.Http.HttpResponseMessage]::new([System.Net.HttpStatusCode]::OK)
        try {
            $message.Content = [System.Net.Http.StringContent]::new($Body, [System.Text.Encoding]::UTF8, 'text/plain')
            $response = [Microsoft.PowerShell.Commands.WebResponseObject]::new($message, [System.TimeSpan]::FromSeconds(10), [System.Threading.CancellationToken]::None)
            $response.ToString() | Should -BeExactly $Expected
            $response.RawContent.EndsWith($Expected) | Should -BeTrue
        } finally {
            $message.Dispose()
        }
    }
}
