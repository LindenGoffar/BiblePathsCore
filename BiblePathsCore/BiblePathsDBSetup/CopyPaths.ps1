<# 
Description: Provides a source DB and Dest DB this script pulls each BiblePath along with Nodes and Stats from the source DB to Dest.

NOTE: This script does not currently set NULL values from the source DB to NUL in the destination.
This is easy enough to fix directly in the DB for consistency as follows: 
	Update [dbo].PathStats set EventData = NULL where EventData = ''
#>

. .\InvokeSQLRemote.ps1

$SourceServer = "biblepaths.database.windows.net"
$SourceDatabase = "BiblePathsDB"
$SourceUser = "BiblePathsDBA"
$SourcePassword = Read-Host "Please Enter the DB Password for source User: $SourceUser"

<#
# Cloud DB connection section... 
$DestServer = "biblepathstaging.database.windows.net"
$DestDatabase = "BiblePathStagingDB"
$DestUser = "StagingDBA"
$DestPassword = Read-Host "Please Enter the DB Password for User: $DestUser"
#>

# Local DB Connection Section... 
$DestServer = "(LocalDb)\MSSQLLocalDB"
$DestDatabase = "C:\Users\linde\Source\Workspaces\Paths\BiblePaths\BiblePathBuilder\App_Data\Local.mdf"



# Open a SQL Connection to Source

$SourceSQLConnection = Open-SqlConnection -ServerInstance $SourceServer -Database $SourceDatabase -Username $SourceUser -Password $SourcePassword 
$DestSQLConnection = Open-SqlConnection -ServerInstance $DestServer -Database $DestDatabase -Username $DestUser -Password $DestPassword 

$GetPathsQuery = @"
        SELECT * FROM [dbo].Paths
"@

$PathID = 1 # This assumes an empty DB to start with
$AllPaths = Invoke-SqlOnConnection -Connection $SourceSQLConnection -Query $GetPathsQuery
ForEach ($Path in $AllPaths) {
    
    $ParameterHash = @{}
       
    $OriginalPathID = $Path.ID
    $Name = $Path.Name
    $ParameterHash.Add("@Name", $Name)
    if ($Path.ComputedRating.GetType().Name -eq "DBNull") { $ComputedRating = 0 } else { $ComputedRating = $Path.ComputedRating }
    $ParameterHash.Add("@ComputedRating", $ComputedRating)
    $Length = $Path.Length
    $ParameterHash.Add("@Length", $Length)
    $Created = $Path.Created
    $ParameterHash.Add("@Created", $Created)
    $Modified = $Path.Modified
    $ParameterHash.Add("@Modified", $Modified)
    $Owner = $Path.Owner
    $ParameterHash.Add("@Owner", $Owner)
    $isPublished = $Path.isPublished
    $ParameterHash.Add("@isPublished", $isPublished)
    $OwnerBibleID = $Path.OwnerBibleID
    $ParameterHash.Add("@OwnerBibleID", $OwnerBibleID)
    $isPublicEditable = $Path.isPublicEditable
    $ParameterHash.Add("@isPublicEditable", $isPublicEditable)
    if ($Path.Topics.GetType().Name -eq "DBNull") { $Topics = "" } else { $Topics = $Path.Topics }
    $ParameterHash.Add("@Topics", $Topics)
    $isDeleted = $Path.isDeleted
    $ParameterHash.Add("@isDeleted", $isDeleted)

    # Add this path to Destination. Note we can't specify ID so we'll just let it set one. 
    $InsertPath = @"
        INSERT INTO Paths (Name, ComputedRating, Length, Created, Modified, Owner, isPublished, OwnerBibleID, isPublicEditable, Topics, isDeleted)
        VALUES (@Name, @ComputedRating, @Length, @Created, @Modified, @Owner, @isPublished, @OwnerBibleID, @isPublicEditable, @Topics, @isDeleted)
"@

    Write-Host "Adding Path: $Name"
    Invoke-SqlOnConnection -Connection $DestSQLConnection -Query $InsertPath -ParameterHash $ParameterHash

    $GetPathNodesQuery = @"
        SELECT * FROM [dbo].PathNodes WHERE PathID = '$OriginalPathID'
"@

    $PathNodes = Invoke-SqlOnConnection -Connection $SourceSQLConnection -Query $GetPathNodesQuery
    $NodeCount = $PathNodes.Count
    Write-Host "   Adding $NodeCount PathNodes"
    ForEach ($Node in $PathNodes){

        $Position = $Node.Position
        $BookNumber = $Node.BookNumber
        $Chapter = $Node.Chapter
        $Start_Verse = $Node.Start_Verse
        $End_Verse = $Node.End_Verse
        $Created = $Node.Created
        $Modified = $Node.Modified

        $InsertPathNode = @"
        INSERT INTO PathNodes (PathID, Position, BookNumber, Chapter, Start_Verse, End_Verse, Created, Modified)
        VALUES ('$PathID', '$Position', '$BookNumber', '$Chapter', '$Start_Verse','$End_Verse','$Created','$Modified')
"@
        Invoke-SqlOnConnection -Connection $DestSQLConnection -Query $InsertPathNode

    }

    $GetPathStatsQuery = @"
        SELECT * FROM [dbo].PathStats WHERE PathID = $OriginalPathID
"@
    $PathStats = Invoke-SqlOnConnection -Connection $SourceSQLConnection -Query $GetPathStatsQuery
    $StatCount = $PathStats.Count
    Write-Host "   Adding $StatCount PathStats"
    ForEach ($Stat in $PathStats){
        
        $EventType = $Stat.EventType
        if ($Stat.EventData.GetType().Name -eq "DBNull") { $EventData = $null } else { $EventData = $Stat.EventData }
        $EventWritten = $Stat.EventWritten

        $InsertPathStat = @"
        INSERT INTO PathStats (PathID, EventType, EventData, EventWritten)
        VALUES ('$PathID', '$EventType', '$EventData', '$EventWritten')
"@
        Invoke-SqlOnConnection -Connection $DestSQLConnection -Query $InsertPathStat        
    }

    # Increment the PathID value... the DB will do this automatically all we need to do is hope these continue to line up. 
    $PathID++
}

# Close the SQL Connection... very important not to skip in debugging. 
close-SqlConnection -Connection $SourceSQLConnection
close-SqlConnection -Connection $DestSQLConnection

        

