#  # ShowUI is a Module to help you create user interfaces in PowerShell

# Using ShowUI, you can create user interaces quickly, cheerly, and nicely, 
# in an amazingly short amount of code.

# This is the smallest 'Hello World' you can do in ShowUI:
label 'Hello World' -Show

# label is an alias to New-Label.  Let's write this the "long" way,
# and add a few options.  
# -Content is the stuff inside of the label.  
#    The content is usually the first parameter
# -FontSize makes it bigger.  
# -FontWeight makes it bolder.
# -FontFamily changes the font used to display the label
# -AsJob runs the label in the background, instead of stopping your script
New-Label 'Hello World' -FontFamily 'Consolas' -FontSize 24 -FontWeight Bold -AsJob

# There are hundreds of built in commands to choose from.
# ShowUI has a command to help you find them:
Get-UICommand

# There plenty of controls to pick from

# You can put up a textbox
New-TextBox -text 'default' -show

# You can make a password box
New-PasswordBox -password 'secrets' -show

# Scribble on an InkCanvas
New-InkCanvas -width 640 -height 480 -show

# Create simple shapes and layout
New-UniformGrid -Width 200 -Height 200 {
    New-Rectangle 'Red'
    New-Rectangle 'Green'
    New-Rectangle 'Blue'   
    New-Rectangle 'Yellow'
} -show

# Create menus
New-Menu -ControlName SampleMenu { 
    New-MenuItem "File" {
        New-MenuItem "E_xit" -on_click { 
            $window.Close()
        } 
    }
} -show

# Display photos
$picture = Get-ChildItem "$env:PUBLIC\Pictures\Sample Pictures" | 
    Get-Random
New-Image -Source "$($picture.Fullname)" -Width 640 -Height 480 -show


# Let's go ahead and try something more practical:
# Let's make a quick UI that will help us get input for a command.
# This uses a UniformGrid, a handy control that comes with ShowUI and WPF
# that lets you automatically put items into a grid
$getCommandInput = UniformGrid -ControlName 'Get-InputForGetCommand' -Columns 2 {
    "Command Name"
    New-TextBox -Name Name
    "Verb"
    New-TextBox -Name Verb
    "Noun"
    New-TextBox -Name Noun
    "In Module"
    New-TextBox -Name Module  
    " " # Some Empty Space
    New-Button "Get Command" -On_Click {
        Get-ParentControl |
            Set-UIValue -passThru | 
            Close-Control
    }
} -show

Get-Command @getCommandInput

# Let's take the coolness up a notch, and show something that will help provide input 
# for Get-EventLog
$getEventInput = StackPanel -ControlName 'Get-EventLogsSinceDate' {
    New-Label -VisualStyle 'MediumText' "Get Event Logs Since..."
    New-ComboBox -IsEditable:$false -SelectedIndex 0 -Name LogName @("Application", "Security", "System", "Setup")
    Select-Date -Name After
    New-Button "Get Events" -On_Click {
        Get-ParentControl |
            Set-UIValue -passThru | 
            Close-Control
    }
} -show

Get-EventLog @getEventInput

# All right, let's show something a little more practical and a little cooler.
# Let's make a very simple session manager in ShowUI:
New-Grid -ControlName 'SessionManager' -Rows (
    'Auto', # Automatically sized header row
    '1*', # The remaining space will be where the list of sessions is displayed    
    'Auto' # Buttons will go along the bottom    
) {
    "Active Sessions"
    New-ListView -Row 1 -Name SessionList -View {
        New-GridView -Columns {
            New-GridViewColumn -Header 'Id' -DisplayMemberBinding 'Id'
            New-GridViewColumn 'Name'
            New-GridViewColumn 'ComputerName'
            New-GridViewColumn 'ConfigurationName'
            New-GridViewColumn 'State'
        }
    }
    
    New-UniformGrid -Row 2 -Rows 1 {
        New-Button "_Open" -Name OpenSession -On_Click {
            # It's very easy to do nested dialogs.  Simply create a new control inside of
            # an event handler, and use -Show.
            $sessionParameters = New-UniformGrid -ControlName 'Get-RemoteSessionOption' -Columns 2 {
                'ComputerName'
                New-TextBox -Name 'ComputerName'
                'ConfigurationName'                
                New-TextBox -Name 'ConfigurationName'
                'Authentication'
                New-ComboBox -Name 'Authentication' -SelectedIndex 0 -ItemsSource {
                    [Enum]::GetValues([Management.Automation.Runspaces.AuthenticationMechanism])
                }
                New-Button "Connect" -On_Click {
                    $parent |
                        Update-UIValue -passThru |
                        Close-Control
                }
                New-Button "Connect As..." -On_Click {
                    $parent | Update-UIValue
                    $parent.Tag.Credential = Get-Credential
                    
                    $parent | Close-Control                                        
                }
            } -show
            if ($sessionParameters) {
                New-PSSession @sessionParameters
                $sessionList.ItemsSource = @(Get-PSSession)   
            }
        }
        New-Button "_Close" -On_Click {
            $sessionList.SelectedItem | Remove-PSSession
            $sessionList.ItemsSource = @(Get-PSSession)    
        }
    }            
} -On_Loaded {
    $sessionList.ItemsSource = @(Get-PSSession)    
} -show