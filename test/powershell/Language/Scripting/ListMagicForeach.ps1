Describe 'Magic Foreach works with List[T]' -Tags "CI" {
    It 'Calls magic scriptblock for each item' {
		[int[]] $i = 1..10
		$list = [System.Collections.Generic.List[int]]::new($i)		
		$sum = 0
		$list.Foreach{$sum += $_ }
		$sum | Should be 55
	}	

	It 'Calls List[T].Foreach when argument is Action' {
		[int[]] $i = 1..10
		$list = [System.Collections.Generic.List[int]]::new($i)		
		$sum = 0
		class CountHelper {
			static [int] $I
		}
		[CountHelper]::I = 0
		[Action[int]] $action = {param($i) [CountHelper]::I += $i }
		$list.Foreach($action)
		[CountHelper]::I | Should be 55
	}

	It 'Calls magic item method when argument is methodName' {
		[int[]] $i = 1..3
		$list = [System.Collections.Generic.List[int]]::new($i)		
		$sum = 0

		$list.Foreach('ToString') -join ',' | Should be "1,2,3"		
	}

	It 'Calls magic item method when argument is methodName + args ' {
		[int[]] $i = 9,10,11
		$list = [System.Collections.Generic.List[int]]::new($i)		
		$sum = 0

		$list.Foreach('ToString', 'x') -join ',' | Should be "9,a,b"		
	}
}
