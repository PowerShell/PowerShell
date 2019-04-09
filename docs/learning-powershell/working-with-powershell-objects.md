# Working with PowerShell Objects

When cmdlets are executed in PowerShell, the output is an Object, as opposed to only returning text.
This provides the ability to store information as properties.
As a result, handling large amounts of data and getting only specific properties is a trivial task.

As a simple example, the following function retrieves information about storage Devices on a Linux or MacOS operating system platform.
This is accomplished by parsing the output of an existing command, *parted -l* in administrative context, and creating an object from the raw text by using the *New-Object* cmdlet.

```powershell
function Get-DiskInfo
{
    $disks = sudo parted -l | Select-String "Disk /dev/sd*" -Context 1,0
    $diskinfo = @()
    foreach ($disk in $disks) {
        $diskline1 = $disk.ToString().Split("`n")[0].ToString().Replace('  Model: ','')
        $diskline2 = $disk.ToString().Split("`n")[1].ToString().Replace('> Disk ','')
        $i = New-Object psobject -Property @{'Friendly Name' = $diskline1; Device=$diskline2.Split(': ')[0]; 'Total Size'=$diskline2.Split(':')[1]}
        $diskinfo += $i
    }
    $diskinfo
}
```

Execute the function and store the results as a variable.
Now retrieve the value of the variable.
The results are formatted as a table with the default view.

*Note: in this example, the disks are virtual disks in a Microsoft Azure virtual machine.*

```powershell
PS /home/psuser> $d = Get-DiskInfo
[sudo] password for psuser:
PS /home/psuser> $d

Friendly Name            Total Size Device
-------------            ---------- ------
Msft Virtual Disk (scsi)  31.5GB    /dev/sda
Msft Virtual Disk (scsi)  145GB     /dev/sdb

```

Passing the variable down the pipeline to *Get-Member* reveals available methods and properties.
This is because the value of *$d* is not just text output.
The value is actually an array of .Net objects with methods and properties.
The properties include Device, Friendly Name, and Total Size.

```powershell
PS /home/psuser> $d | Get-Member


   TypeName: System.Management.Automation.PSCustomObject

Name          MemberType   Definition
----          ----------   ----------
Equals        Method       bool Equals(System.Object obj)
GetHashCode   Method       int GetHashCode()
GetType       Method       type GetType()
ToString      Method       string ToString()
Device        NoteProperty string Device=/dev/sda
Friendly Name NoteProperty string Friendly Name=Msft Virtual Disk (scsi)
Total Size    NoteProperty string Total Size= 31.5GB
```

To confirm, we can call the GetType() method interactively from the console.

```powershell
PS /home/psuser> $d.GetType()

IsPublic IsSerial Name                                     BaseType
-------- -------- ----                                     --------
True     True     Object[]                                 System.Array
```

To index in to the array and return only specific objects, use the square brackets.

```powershell
PS /home/psuser> $d[0]

Friendly Name            Total Size Device
-------------            ---------- ------
Msft Virtual Disk (scsi)  31.5GB    /dev/sda

PS /home/psuser> $d[0].GetType()

IsPublic IsSerial Name                                     BaseType
-------- -------- ----                                     --------
True     False    PSCustomObject                           System.Object
```

To return a specific property, the property name can be called interactively from the console.

```powershell
PS /home/psuser> $d.Device
/dev/sda
/dev/sdb
```

To output a view of the information other than default, such as a view with only specific properties selected, pass the value to the *Select-Object* cmdlet.

```powershell
PS /home/psuser> $d | Select-Object Device, 'Total Size'

Device   Total Size
------   ----------
/dev/sda  31.5GB
/dev/sdb  145GB
```

Finally, the example below demonstrates use of the *ForEach-Object* cmdlet to iterate through the array and manipulate the value of a specific property of each object.
In this case the Total Size property, which was given in Gigabytes, is changed to Megabytes.
Alternatively, index in to a position in the array as shown below in the third example.

```powershell
PS /home/psuser> $d | ForEach-Object 'Total Size'
 31.5GB
 145GB

PS /home/psuser> $d | ForEach-Object {$_.'Total Size' / 1MB}
32256
148480

PS /home/psuser> $d[1].'Total Size' / 1MB
148480
```
