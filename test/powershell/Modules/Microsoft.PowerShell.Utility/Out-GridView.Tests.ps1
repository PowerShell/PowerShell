# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Out-GridView search" -Tags "CI" {
    It "Preserves spaces while filtering as each character is entered" -Skip:(![System.Management.Automation.Platform]::IsWindowsDesktop) {
        Add-Type -AssemblyName Microsoft.PowerShell.GraphicalHost
        $view = [Microsoft.Management.UI.Internal.ManagementList]::new()
        $view.List.ItemsSource = [string[]]('a', 'b only', 'b c')
        $search = $view.SearchBox
        $search.Parser.FullTextRule = [Microsoft.Management.UI.Internal.TextContainsFilterRule]::new()

        # Attach the search box to a WPF tree so it uses its normal template without opening a window.
        $parent = [System.Windows.Controls.Border]::new()
        $parent.Child = $search
        $null = $search.ApplyTemplate()
        $editor = $search.Template.FindName('PART_SearchBox', $search)

        $editor.AppendText('b')
        @($view.List.Items) | Should -Be @('b only', 'b c')
        $editor.AppendText(' ')
        $editor.Text | Should -BeExactly 'b '
        $editor.AppendText('c')
        $editor.Text | Should -BeExactly 'b c'
        @($view.List.Items) | Should -Be @('b c')

        $editor.SelectAll()
        $editor.SelectedText = ' '
        $editor.Text | Should -BeExactly ' '
        $search.HasFilterExpression | Should -BeFalse
        @($view.List.Items) | Should -Be @('a', 'b only', 'b c')
        $clear = [Microsoft.Management.UI.Internal.SearchBox]::ClearTextCommand
        $clear.CanExecute($null, $editor) | Should -BeTrue
        $clear.Execute($null, $editor)
        $editor.Text | Should -BeExactly ''
    }
}
