
# load for Prod
# $BaseURI = "https://biblepaths.net"

# $BaseURI = "https://www.biblepaths.net"
# load for Test
# $BaseURI = "https://biblepathsppe.azurewebsites.net"

# load for Local Debug
$BaseURI = "https://localhost:44387"

# Get all bibles
Invoke-RestMethod -Method Get -Uri "$BaseURI/API/Bibles"

# Get a specific bible
Invoke-RestMethod -Method Get -Uri "$BaseURI/API/Bibles/NKJV-EN"

# Get Bible Chapters
Invoke-RestMethod -Method Get -Uri "$BaseURI/API/BibleChapters/?BibleId=NKJV-EN&BookName=Genesis"


try {
    Invoke-RestMethod -Method Get -Uri "$BaseURI/API/Bibles/KJV-EN"
} catch {
    # Dig into the exception to get the Response details.
    # Note that value__ is not a typo.
    Write-Host "StatusCode:" $_.Exception.Response.StatusCode.value__ 
    Write-Host "StatusDescription:" $_.Exception.Response.StatusDescription
    $Exception = $_.Exception
}

# Get all Paths.
Invoke-RestMethod -Method Get -Uri "$BaseURI/API/Paths"

# Get a specific Path by ID 
Invoke-RestMethod -Method Get -Uri "$BaseURI/API/Paths/1"

# BibleVerses
$BibleID = "KJV-EN"
$BookNumber = 43 #John
$Chapter = 3
$Verse = 16
$Verse2 = 17
Invoke-RestMethod -Method Get -Uri "$BaseURI/API/BibleVerses/?BibleID=$BibleID&BookNumber=$BookNumber&Chapter=$Chapter&StartVerse=$Verse&EndVerse=$Verse2"


# QuizQuestions
Invoke-RestMethod -Method Get -Uri "$BaseURI/API/QuizQuestions/?BibleId=NKJV-EN&BookName=Genesis&Chapter=1"


#QuizQuestions Commentary
Invoke-RestMethod -Method Get -Uri "$BaseURI/API/QuizQuestions/?BibleID=NKJV-EN&BookName=Ezra&Chapter=1000"


# Add QuizQuestion
# To obtain a valid Token browse to ../howto/apitoken
    $APIToken = "..."
    $OwnerEmail = "..."
    

        $QuestionObj = New-Object -TypeName psobject
        $QuestionObj | Add-Member -MemberType NoteProperty -Name BibleId -value "NKJV-EN"
        $QuestionObj | Add-Member -MemberType NoteProperty -Name question -value "The earth was described as Formless and What?"
        $QuestionObj | Add-Member -MemberType NoteProperty -Name points -value 1
        $QuestionObj | Add-Member -MemberType NoteProperty -Name booknumber -value 1
        $QuestionObj | Add-Member -MemberType NoteProperty -name chapter -value 1
        $QuestionObj | Add-Member -MemberType NoteProperty -name startverse -value 2
        $QuestionObj | Add-Member -MemberType NoteProperty -name endverse -value 2
        $QuestionObj | Add-member -MemberType NoteProperty -Name owner -value $OwnerEmail
        $QuestionObj | Add-member -MemberType NoteProperty -Name token -value $APIToken
        $QuestionObj | Add-Member -MemberType NoteProperty -Name source -value "API Test"

        $AcceptedAnswers = @() # each question can have multiple answers we'll add one.
            $AcceptedAnswers += "empty"
        $QuestionObj | Add-Member -MemberType NoteProperty -Name answers -value $AcceptedAnswers 

        # Finally add our question object
        $JsonString = ConvertTo-Json -InputObject $QuestionObj
        Invoke-RestMethod -Method Post -Uri "$BaseURI/API/QuizQuestions" -Body $JsonString -ContentType "application/json"


# Generate Fill in The Blank Questions
Invoke-RestMethod -Method Get -Uri "$BaseURI/API/FitBQuestion/?BibleId=NKJV-EN&BookName=Genesis&Chapter=1&Verse=1"  


# NOT YET IMPLIMENTED

# Get the 2 newest Paths.
Invoke-RestMethod -Method Get -Uri "$BaseURI/API/BiblePaths/?SortOrder=Newest&Top=2"

# Get a Step by it's ID, specifying Bible Language and Version.
$StepID = 2
$BibleID = "KJV-EN"
Invoke-RestMethod -Method Get -Uri "$BaseURI/API/BibleSteps/$StepID/?BibleID=$BibleID"

# Get a specific Book/Chapter combination. 
$StepID = 2
$BibleID = "KJV-EN"
$BookNumber = 43
$Chapter = 3
(Invoke-RestMethod -Method Get -Uri "$BaseURI/API/BibleSteps/?ID=$StepID&BibleID=$BibleID&BookNumber=$BookNumber&Chapter=$Chapter").verses


# End2End Navigate Forward.
$BibleID = "KJV-EN"
$Path = Invoke-RestMethod -Method Get -Uri "$BaseURI/API/BiblePaths/8"
$FirstStepID = $Path.FirstStepID
$NextStepID = $FirstStepID
while ($NextStepID -ne 0){ # StepID = 0 indicates we've reached the beginning or end
    $Step = Invoke-RestMethod -Method Get -Uri "$BaseURI/API/BibleSteps/$NextStepID/?BibleID=$BibleID"
    Write-host "-----"
    Write-host $Step.BookName $Step.Chapter
    Write-host "==>"
    $NextStepID = $Step.FWStepID
}

# End2End Navigate Up once.
$BibleID = "KJV-EN"
$PathID= "ForGodSoLovedTheWorld"
$Path = Invoke-RestMethod -Method Get -Uri "$BaseURI/API/BiblePaths/$PathID"
$FirstStepID = $Path.FirstStepID
$Step = Invoke-RestMethod -Method Get -Uri "$BaseURI/API/BibleSteps/$FirstStepID/?BibleID=$BibleID"
Write-host "-----"
Write-host $Step.BookName $Step.Chapter
Write-host "^"
$CurrentBook = $Step.BookNumber
if ($Step.Chapter -gt 1){
    $PrevChapter = ($Step.Chapter - 1)
    $TempStep = Invoke-RestMethod -Method Get -Uri "$BaseURI/API/BibleSteps/?BibleID=$BibleID&BookNumber=$CurrentBook&Chapter=$PrevChapter"
    Write-host "-----"
    Write-host $TempStep.BookName $TempStep.Chapter
    Write-host "====="
}

# BibleStudy... Get John 3.
$BibleID = "KJV-EN"
$BookNumber = 43 #John
$Chapter = 3
(Invoke-RestMethod -Method Get -Uri "$BaseURI/API/BibleStudy/?BibleID=$BibleID&BookNumber=$BookNumber&Chapter=$Chapter").verses

# BibleVerses
$BibleID = "KJV-EN"
$BookNumber = 43 #John
$Chapter = 3
$Verse = 16
$Verse2 = 17
Invoke-RestMethod -Method Get -Uri "$BaseURI/API/BibleVerses/?BibleID=$BibleID&BookNumber=$BookNumber&Chapter=$Chapter&Start_Verse=$Verse&End_Verse=$Verse2"

# QuizStats

Invoke-RestMethod -Method Get -Uri "$BaseURI/API/QuizStats/GetUserSummary/?id=1"

# QuizQuestions
Invoke-RestMethod -Method Get -Uri "$BaseURI/API/PBEQuestions/GetQuestions/?BookName=Luke&Chapter=1"

#QuizQuestions Commentary
Invoke-RestMethod -Method Get -Uri "$BaseURI/API/PBEQuestions/GetQuestions/?BookName=Ezra&Chapter=1000"

# Get Quiz Book QuestionStats
Invoke-RestMethod -Method Get -Uri "$BaseURI/API/PBEQuestions/GetBookStats"

# Get Quiz Book QuestionStats
Invoke-RestMethod -Method Get -Uri "$BaseURI/API/PBEQuestions/GetGroupStats/?GroupID=2"

# ARCHIVE------------------- 

Invoke-RestMethod -Method Get -Uri 'http://localhost:6501/API/Test'?

Invoke-RestMethod -Method Get -Uri 'http://localhost:6501/API/Test/KJV-EN'

Invoke-RestMethod -Method Get -Uri 'http://localhost:6501/API/BibleVerses'

Invoke-RestMethod -Method Get -Uri "http://localhost:6501/API/BibleVerses?$filter=BookName eq 'John"

Invoke-RestMethod -Method Get -Uri 'http://localhost:6501/API/PathNodes' -Body $Path

Invoke-RestMethod -Method Get -Uri 'http://localhost:6501/API/BibleSteps/1/?Language=English&Version=King James Version'