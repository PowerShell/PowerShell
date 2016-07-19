Describe "Junctions" -Tags "Feature" {
    BeforeAll {
        if ( $IsWindows ) {
            $skip = $false
        }
        else {
            $skip = $true
        }
    }
    Context "Cyclic Junctions" {
        BeforeAll { 
            if ( ! $skip ) {
                $junctionTarget = (New-Item -Path "TestDrive:\JunctionTarget" -ItemType Directory -Force).FullName
                $null = New-Item -Path "TestDrive:\JunctionTarget\file.txt"
                $cyclicJunction = (New-Item -Path "TestDrive:\JunctionTarget\Cyclic" -target $junctionTarget -ItemType junction -Force).fullName
            }
        }

        It -skip:$skip "copy junction with force gets an error" {
            Copy-Item $cyclicJunction -Recurse -Destination "TestDrive:\CopyDestination" -force -ErrorAction SilentlyContinue -ErrorVariable copyErrorDirectory
            $copyErrorDirectory.FullyQualifiedErrorId | Should Be "CopyDirectoryInfoItemIOError,Microsoft.PowerShell.Commands.CopyItemCommand"
        }
               
        It -skip:$skip "cannot be deleted without Force" {
            Remove-Item $cyclicJunction -Recurse -ErrorVariable cyclicDeleteError -ErrorAction SilentlyContinue            
            $cyclicDeleteError.FullyQualifiedErrorId | Should Be "DirectoryNotEmpty,Microsoft.PowerShell.Commands.RemoveItemCommand"    
        }

        It -skip:$skip "can be deleted" {
            Remove-Item $cyclicJunction -Force -Recurse
            $cyclicJunction | Should Not Exist
        }
    }

    Context "Junction operations" {
        BeforeAll {
            if ( ! $skip  ) {
                $junctionTarget = (New-Item -Path "TestDrive:\JunctionTarget" -ItemType Directory -Force).FullName
                $null = New-Item -Path "TestDrive:\JunctionTarget\file.txt"
                $junction = (New-Item -Path "TestDrive:\JunctionDestination" -target $junctionTarget -ItemType junction -Force).fullName
            }
        }
        
        It -skip:$skip "target can be deleted" {
            Remove-Item $junctionTarget -Recurse -Force            
            Get-ChildItem $junction -ErrorVariable targetDelete -ErrorAction SilentlyContinue
            $targetDelete.FullyQualifiedErrorId | Should Be "DirIOError,Microsoft.PowerShell.Commands.GetChildItemCommand"
        }

        AfterAll {
            if ( ! $skip ) {
                Remove-Item $junction -Force -ErrorAction SilentlyContinue
            }
        }
    }
}
