<# 
Description: Create individual or all BiblePaths Core tables.


CRITICAL UPDATES for pre-existing DBs
---------------------------------------

ALTER TABLE BibleNoiseWords
		ADD	Occurs int NOT NULL

ALTER TABLE BibleNoiseWords
		ADD	isNoise BIT NOT NULL DEFAULT 0

ALTER TABLE BibleNoiseWords
		ADD	WordType int NOT NULL

ALTER TABLE GameTeams
		Add GameStarted datetimeoffset,
			
ALTER TABLE GameTeams
		Add GameCompleted datetimeoffset

UPDATE dbo.QuizQuestions
SET BibleID = 'NKJV-EN'
WHERE BibleID is null
---------------------------------------

#>

Param(  #[switch] $SetupSecurity,
        [switch] $CreateBiblesTable,
        [switch] $CreateBibleBooksTable,
		[switch] $CreateBibleNoiseWordsTable,
		[switch] $CreateBibleWordIndexTable,
        [switch] $CreateBibleChaptersTable,
        [switch] $CreateBibleVersesTable,
		[switch] $CreatePathsTable,
        [switch] $CreatePathNodesTable,
        [switch] $CreatePathStatsTable,
        [switch] $CreateQuizTables,
		[switch] $CreateCommentaryTable,
		[switch] $CreatePreDefinedQuizTables,
		[switch] $CreateGameTables,
		[switch] $LocalDB,
        [switch] $ProductionDB,
        [switch] $StagingDB
      )

. .\InvokeSQLRemote.ps1

if ($ProductionDB){
    # Production...
    $Server = "biblepaths.database.windows.net"
    $Database = "BiblePathsDB"
    $User = "BiblePathsDBA"
    $Password = Read-Host "Please Enter the DB Password for User: $User" 
    #>
}

if ($StagingDB){
    # Staging...
    $Server = "biblepathstaging.database.windows.net"
    $Database = "BiblePathStagingDB"
    $User = "StagingDBA"
    $Password = Read-Host "Please Enter the DB Password for User: $User" 
}

if ($LocalDB) {
    # Local DB Connection Section... 
    $Server = "(LocalDb)\MSSQLLocalDB"
    $Database = "BiblePathsApp"
    #>
}

# Check whether we have a viable DB now.
if ($Database.Length -lt 1){
    Write-Host "You must specify a target DB using -LocalDB, -StagingDB or -ProductionDB"
    break
}


If ($SetupSecurity){
	Write-Host "Setting Up Security" 
	$SetupSecurityQuery = @"
		CREATE USER RWACC WITH password='B!bleP@thsStr0ngP@ssw0rd'
		EXEC sp_addrolemember 'db_datawriter', 'RWACC';
		EXEC sp_addrolemember 'db_datawriter', 'RWACC';
		Grant select to RWACC
		Grant CONNECT to RWACC
"@
    Invoke-SqlcmdRemote -ServerInstance $Server -Database $Database -Query $SetupSecurityQuery -Username $User -Password $Password.
}

If ($CreateBiblesTable){
	Write-Host "Creating Bibles Table" 
	$CreateBiblesTableQuery = @"
		CREATE TABLE Bibles
		(
			ID nvarchar(64) PRIMARY KEY,
			Language nvarchar(64) NOT NULL,
			Version nvarchar(64) NOT NULL
		) 
"@
    Invoke-SqlcmdRemote -ServerInstance $Server -Database $Database -Query $CreateBiblesTableQuery -Username $User -Password $Password
}

If ($CreateBibleBooksTable){
	Write-Host "Creating BibleBooks Table" 
	$CreateBibleBooksTableQuery = @"
		CREATE TABLE BibleBooks
		(
            BibleID nvarchar(64) FOREIGN KEY References Bibles(ID),
            Testament nvarchar(32),
            TestamentNumber int,
            BookNumber int,
			Name nvarchar (32), 
			Chapters int,
            CONSTRAINT pk_BookID PRIMARY KEY (BibleID,BookNumber)
		) 
"@
    Invoke-SqlcmdRemote -ServerInstance $Server -Database $Database -Query $CreateBibleBooksTableQuery -Username $User -Password $Password
}

If ($CreateBibleNoiseWordsTable){
	Write-Host "Creating Bible NoiseWords Table" 
	$CreateBibleNoiseWordsTableQuery = @"
		CREATE TABLE BibleNoiseWords
		(
			BibleID nvarchar(64) FOREIGN KEY References Bibles(ID) NOT NULL,
			NoiseWord nvarchar(32) NOT NULL,
			CONSTRAINT pk_NoiseWordID PRIMARY KEY (BibleID,NoiseWord),
			Occurs int NOT NULL,
			isNoise BIT NOT NULL DEFAULT 0,
			WordType int NOT NULL
        )
"@
    Invoke-SqlcmdRemote -ServerInstance $Server -Database $Database -Query $CreateBibleNoiseWordsTableQuery -Username $User -Password $Password
}

If ($CreateBibleChaptersTable){
	Write-Host "Creating BibleChapters Table" 
	$CreateBibleChaptersTableQuery = @"
		CREATE TABLE BibleChapters
		(
            BibleID nvarchar(64),
            BookNumber int, 
			Name nvarchar (32), 
			ChapterNumber int,
            Verses int,
            CONSTRAINT pk_BookChapterID PRIMARY KEY (BibleID,BookNumber,ChapterNumber),
            CONSTRAINT FK_BibleID_BookNumber FOREIGN KEY (BibleID,BookNumber) REFERENCES dbo.Biblebooks (BibleID,BookNumber)
		) 
"@
    Invoke-SqlcmdRemote -ServerInstance $Server -Database $Database -Query $CreateBibleChaptersTableQuery -Username $User -Password $Password
}

If ($CreateBibleVersesTable){
	Write-Host "Creating Bible Verses Table" 
	$CreateBibleVersesTableQuery = @"
		CREATE TABLE BibleVerses
		(
			ID int IDENTITY(1,1) PRIMARY KEY,
			BibleID nvarchar(64) FOREIGN KEY References Bibles(ID),
			Testament nvarchar(32),
			BookNumber int NOT NULL,
            BookName nvarchar (32),
			Chapter int NOT NULL,
			Verse int NOT NULL,
			Text nvarchar(2048)
		) 
"@
    Invoke-SqlcmdRemote -ServerInstance $Server -Database $Database -Query $CreateBibleVersesTableQuery -Username $User -Password $Password
}

If ($CreatePathsTable){
	Write-Host "Creating Paths Table" 
	$CreatePathsTableQuery = @"
		CREATE TABLE Paths
		(
			ID int IDENTITY(1,1) PRIMARY KEY,
			Name nvarchar(256),
			CONSTRAINT AK_Name UNIQUE(Name), 
			Length int NOT NULL,
			ComputedRating decimal(16,8),
			Created datetimeoffset,
			Modified datetimeoffset,
			Owner nvarchar(256), 
			OwnerBibleID nvarchar(64),
			Topics nvarchar(256),
			isPublished BIT NOT NULL DEFAULT 0, 
			isPublicEditable BIT NOT NULL DEFAULT 0,
			isDeleted BIT NOT NULL DEFAULT 0,
			StepCount int NOT NULL DEFAULT 0,
			Reads int NOT NULL DEFAULT 0
		) 
"@
	<# adding the Reads column manually 
		ALTER TABLE dbo.Paths
		ADD Reads int NOT NULL DEFAULT 0
	#> 
    Invoke-SqlcmdRemote -ServerInstance $Server -Database $Database -Query $CreatePathsTableQuery -Username $User -Password $Password
}

If ($CreatePathNodesTable){
	Write-Host "Creating Path Nodes Table" 
	$CreatePathNodesTableQuery = @"
		CREATE TABLE PathNodes
		(
			ID int IDENTITY(1,1) PRIMARY KEY,
			PathID int FOREIGN KEY References Paths(ID),
			Position int NOT NULL,
			BookNumber int NOT NULL,
			Chapter int NOT NULL,
			Start_Verse int NOT NULL,
			End_Verse int NOT NULL,
			Created datetimeoffset,
			Modified datetimeoffset
		) 
"@
    Invoke-SqlcmdRemote -ServerInstance $Server -Database $Database -Query $CreatePathNodesTableQuery -Username $User -Password $Password
}

If ($CreatePathStatsTable){
	Write-Host "Creating Path Stats Table" 
	$CreatePathStatsTableQuery = @"
		CREATE TABLE PathStats
		(
			ID int IDENTITY(1,1) PRIMARY KEY,
			PathID int FOREIGN KEY References Paths(ID),
			EventType int NOT NULL,
			EventData nvarchar(2048),
			EventWritten datetimeoffset
		) 
"@
    Invoke-SqlcmdRemote -ServerInstance $Server -Database $Database -Query $CreatePathStatsTableQuery -Username $User -Password $Password
}
If ($CreateQuizTables){
	Write-Host "Creating Quiz Tables" 
	$CreateQuizUsersTableQuery = @"
		CREATE TABLE QuizUsers
		(
			ID int IDENTITY(1,1) PRIMARY KEY,
            Email nvarchar(256),
            isQuestionBuilderLocked BIT NOT NULL DEFAULT 0,
            isQuizTakerLocked BIT NOT NULL DEFAULT 0,
            isModerator BIT NOT NULL DEFAULT 0,
            Added datetimeoffset,
			Modified datetimeoffset
		) 
"@
	$CreateQuizQuestionsTableQuery = @"
		CREATE TABLE QuizQuestions
		(
			ID int IDENTITY(1,1) PRIMARY KEY,
			BibleID nvarchar(64) FOREIGN KEY References Bibles(ID),
            Question nvarchar(2048),
            Owner nvarchar(256),
            Challenged BIT NOT NULL DEFAULT 0,
            ChallengedBy nvarchar(256),
            ChallengeComment nvarchar(1024),
            isAnswered BIT NOT NULL DEFAULT 0,
            isDeleted BIT NOT NULL DEFAULT 0,
			Points int NOT NULL DEFAULT 1,
			BookNumber int NOT NULL,
			Chapter int NOT NULL,
			Start_Verse int NOT NULL,
			End_Verse int NOT NULL,
			Created datetimeoffset,
			Modified datetimeoffset,
			Source nvarchar(256),
			LastAsked datetimeoffset NOT NULL DEFAULT ('2001-01-01')
		) 
"@
	$CreateQuizAnswersTableQuery = @"
		CREATE TABLE QuizAnswers
		(
			ID int IDENTITY(1,1) PRIMARY KEY,
            QuestionID int FOREIGN KEY References QuizQuestions(ID),
            Answer nvarchar(1024),
			isPrimary BIT NOT NULL DEFAULT 0,
			Created datetimeoffset,
			Modified datetimeoffset
		) 
"@
	$CreateQuizQuestionStatsQuery = @"
		CREATE TABLE QuizQuestionStats
		(
			ID int IDENTITY(1,1) PRIMARY KEY,
			QuestionID int FOREIGN KEY References QuizQuestions(ID),
            QuizUserID int FOREIGN KEY References QuizUsers(ID),
			QuizGroupID int, 
			Points int,
			EventType int NOT NULL,
			EventData nvarchar(2048),
			EventWritten datetimeoffset
		) 
"@
	$CreateQuizGroupStatsQuery = @"
		CREATE TABLE QuizGroupStats
		(
			ID int IDENTITY(1,1) PRIMARY KEY,
            QuizUserID int FOREIGN KEY References QuizUsers(ID),
			GroupName nvarchar(2048),
			BookNumber int NOT NULL,
			QuestionsAsked int NOT NULL,
			PointsPossible int NOT NULL,
			PointsAwarded int NOT NULL,
			PredefinedQuiz int NOT NULL DEFAULT 0, 
			Created datetimeoffset,
			Modified datetimeoffset,
			isDeleted BIT NOT NULL DEFAULT 0
		) 
"@
	$CreateQuizBookListsQuery = @"
		CREATE TABLE QuizBookLists
		(
			ID int IDENTITY(1000,1) PRIMARY KEY,
            BookListName nvarchar(2048),
			Created datetimeoffset,
			Modified datetimeoffset,
			isDeleted BIT NOT NULL DEFAULT 0
		) 
"@
	$CreateQuizBookListBookMapQuery = @"
		CREATE TABLE QuizBookListBookMap
		(
			ID int IDENTITY(1,1) PRIMARY KEY,
            BookListID int FOREIGN KEY References QuizBookLists(ID),
			BookNumber int NOT NULL,
			Created datetimeoffset,
			Modified datetimeoffset,
			isDeleted BIT NOT NULL DEFAULT 0
		) 
"@

    Write-Host "Creating QuizUsers Table" 
    Invoke-SqlcmdRemote -ServerInstance $Server -Database $Database -Query $CreateQuizUsersTableQuery -Username $User -Password $Password
    Write-Host "Creating QuizQuestions Table" 
    Invoke-SqlcmdRemote -ServerInstance $Server -Database $Database -Query $CreateQuizQuestionsTableQuery -Username $User -Password $Password
    Write-Host "Creating QuizAnswers Table" 
    Invoke-SqlcmdRemote -ServerInstance $Server -Database $Database -Query $CreateQuizAnswersTableQuery -Username $User -Password $Password
    Write-Host "Creating QuizQuestionStats Table" 
    Invoke-SqlcmdRemote -ServerInstance $Server -Database $Database -Query $CreateQuizQuestionStatsQuery -Username $User -Password $Password
	Write-Host "Creating QuizGroupStats Table" 
    Invoke-SqlcmdRemote -ServerInstance $Server -Database $Database -Query $CreateQuizGroupStatsQuery -Username $User -Password $Password
	Write-Host "Creating QuizBookLists Table" 
    Invoke-SqlcmdRemote -ServerInstance $Server -Database $Database -Query $CreateQuizBookListsQuery -Username $User -Password $Password
	Write-Host "Creating QuizBookListBookMap Table" 
    Invoke-SqlcmdRemote -ServerInstance $Server -Database $Database -Query $CreateQuizBookListBookMapQuery -Username $User -Password $Password
}
If ($CreateCommentaryTable){
	Write-Host "Creating Commentary Books Table" 
	$CreateCommentaryBooksTableQuery = @"
		CREATE TABLE CommentaryBooks
		(
			ID int IDENTITY(1,1) PRIMARY KEY,
			BibleID nvarchar(64) FOREIGN KEY References Bibles(ID),
			BookNumber int NOT NULL,
            BookName nvarchar (32),
			Text nvarchar(MAX)
		) 
"@
    Invoke-SqlcmdRemote -ServerInstance $Server -Database $Database -Query $CreateCommentaryBooksTableQuery -Username $User -Password $Password
}
If ($CreatePreDefinedQuizTables){
	$CreatePredefinedQuizTableQuery = @"
		CREATE TABLE PredefinedQuizzes
		(
			ID int IDENTITY(1,1) PRIMARY KEY,
			QuizUserID int FOREIGN KEY References QuizUsers(ID),
			QuizName nvarchar (2048), 
			BookNumber int NOT NULL,
			NumQuestions int NOT NULL,
			Created datetimeoffset,
			Modified datetimeoffset,
			isDeleted BIT NOT NULL DEFAULT 0
		) 
"@
	$CreatePredefinedQuizQuestionsTableQuery = @"
		CREATE TABLE PredefinedQuizQuestions
		(
			ID int IDENTITY(1,1) PRIMARY KEY,
			PredefinedQuizID int FOREIGN KEY References PredefinedQuizzes(ID),
			QuestionNumber int NOT NULL,
			BookNumber int NOT NULL,
			Chapter int NOT NULL
		) 
"@
	Write-Host "Creating Predefined Quiz Table" 
    Invoke-SqlcmdRemote -ServerInstance $Server -Database $Database -Query $CreatePredefinedQuizTableQuery -Username $User -Password $Password
	Write-Host "Creating Predefined Quiz Question Table" 
    Invoke-SqlcmdRemote -ServerInstance $Server -Database $Database -Query $CreatePredefinedQuizQuestionsTableQuery -Username $User -Password $Password
}

If ($CreateGameTables){
	$CreateGameGroupsTableQuery = @"
		CREATE TABLE GameGroups
		(
			ID int IDENTITY(1,1) PRIMARY KEY,
			Name nvarchar(256),
			Owner nvarchar(256),
			PathID int FOREIGN KEY References Paths(ID),
			GroupType int NOT NULL,
			GroupState int NOT NULL,
			Created datetimeoffset,
			Modified datetimeoffset
		) 
"@
	$CreateGameTeamsTableQuery = @"
		CREATE TABLE GameTeams
		(
			ID int IDENTITY(1,1) PRIMARY KEY,
			GroupID int FOREIGN KEY References GameGroups(ID),
			Name nvarchar(256), 
			CurrentStepID int NOT NULL,
			StepNumber int NOT NULL, 
			TeamType int NOT NULL,
			BoardState int NOT NULL,
			KeyWord nvarchar(256), 
			GuideWord nvarchar(256), 
			Created datetimeoffset,
			Modified datetimeoffset,
			GameStarted datetimeoffset,
			GameCompleted datetimeoffset
		) 
"@
	Write-Host "Creating GameGroups Quiz Table" 
    Invoke-SqlcmdRemote -ServerInstance $Server -Database $Database -Query $CreateGameGroupsTableQuery -Username $User -Password $Password
	Write-Host "Creating GameTeams Table" 
    Invoke-SqlcmdRemote -ServerInstance $Server -Database $Database -Query $CreateGameTeamsTableQuery -Username $User -Password $Password
}
If ($CreateBibleWordIndexTable){
	$CreateBibleWordIndexQuery = @"
		CREATE TABLE BibleWordIndex
		(
			ID int IDENTITY(1,1) PRIMARY KEY,
			BibleID nvarchar(64) FOREIGN KEY References Bibles(ID) NOT NULL,
			Word nvarchar(32) NOT NULL,
			VerseID int NOT NULL
		) 
"@
	Write-Host "Creating BibleWordIndex Table" 
    Invoke-SqlcmdRemote -ServerInstance $Server -Database $Database -Query $CreateBibleWordIndexQuery -Username $User -Password $Password
}