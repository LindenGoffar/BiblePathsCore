# load for Local Debug
$TargetURI = "https://localhost:44387"
#$TargetURI = "https://biblepathsppe.azurewebsites.net"

$SourceURI = "https://biblepaths.net"
#$SourceURI = "https://biblepathstaging.azurewebsites.net"

$SelectedBooks = @("Ruth","1 Kings")
# $SelectedBooks = @("James")

# Add QuizQuestion
# To obtain a valid Token browse to ../howto/apitoken
    $APIToken = "bGdvZmZhckBnbWFpbC5jb20zNjUzNg=="
    $OwnerEmail = "lgoffar@gmail.com"


$Questions = @()
$QuestionsforImport = @()
$FailedQuestions = @()


<#=====================================================
BEGIN MAIN
=======================================================
#>

# Retrieve questions. 
Write-Host "Retrieving Bible Info" 
$NKJVBible = Invoke-RestMethod -Method Get -Uri "$SourceURI/API/Bibles/NKJV-EN"

Write-host "Retrieving Questions"
Foreach ($Book in $SelectedBooks){
    $BookObj = $NKJVBible.BibleBooks | Where-Object {$_.Name.ToLower() -eq $Book.Tolower()}
    $ChapterCount = $BookObj.Chapters
    $BookName = $BookObj.Name

    Write-host "Processing $BookName with $ChapterCount Chapters"
    For ($i=1; $i -le $ChapterCount; $i++){
        $Questions += Invoke-RestMethod -Method Get -Uri "$SourceURI/API/QuizQuestions/?BibleId=NKJV-EN&BookName=$BookName&Chapter=$i"
    }
}

Write-Host "Adding questions for import" 
Foreach ($Question in $Questions)
{
    if (($Question.answers.Count -gt 0) -and ($Question.question.Length -gt 3))
    {
        $QuestionObj = New-Object -TypeName psobject
        $QuestionObj | Add-Member -MemberType NoteProperty -Name BibleId -value "NKJV-EN"
        $QuestionObj | Add-Member -MemberType NoteProperty -Name question -value $Question.question
        $QuestionObj | Add-Member -MemberType NoteProperty -Name points -value $Question.points
        $QuestionObj | Add-Member -MemberType NoteProperty -Name booknumber -value $Question.bookNumber
        $QuestionObj | Add-Member -MemberType NoteProperty -name chapter -value $Question.chapter
        $QuestionObj | Add-Member -MemberType NoteProperty -name startverse -value $Question.startVerse
        $QuestionObj | Add-Member -MemberType NoteProperty -name endverse -value $Question.endVerse
        $QuestionObj | Add-member -MemberType NoteProperty -Name owner -value $OwnerEmail
        $QuestionObj | Add-member -MemberType NoteProperty -Name token -value $APIToken
        $QuestionObj | Add-Member -MemberType NoteProperty -Name source -value "LoadQuestions Script"

        $AcceptedAnswers = @() # each question can have multiple answers.
        foreach ($Answer in $Question.answers)
        {
            $AcceptedAnswers += $Answer
        }
        $QuestionObj | Add-Member -MemberType NoteProperty -Name answers -value $AcceptedAnswers 

        # Finally add our question object
        $QuestionsforImport += $QuestionObj
    }     
}

# OK now we've got our Questions let's try adding each one to BiblePaths.net/API/PBEQuestions

foreach ($QuestionObj in $QuestionsforImport)
{
    $JsonString = ConvertTo-Json -InputObject $QuestionObj
    Write-host "Adding Question"
    # We are going to optimistically try to add each question obj 
    try {
        Invoke-RestMethod -Method Post -Uri "$TargetURI/API/QuizQuestions" -Body $JsonString -ContentType "application/json"
    }
    catch {
        # OK this seems to have failed so let's just save out the $QuestionObj 
        $StatusCode =  $_.Exception.Response.StatusCode.value__ 
        $StatusDescription = $_.Exception.Response.StatusDescription
        Write-host " Failed to add question"

        $QuestionObj | Add-Member -MemberType NoteProperty -Name StatusCode -value $StatusCode
        $QuestionObj | Add-Member -MemberType NoteProperty -Name StatusDescription -value $StatusDescription
        $FailedQuestions += $QuestionObj
    }
}



