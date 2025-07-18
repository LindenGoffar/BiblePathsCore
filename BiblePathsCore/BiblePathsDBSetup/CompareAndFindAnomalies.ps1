Param(
        [ValidateScript({Test-Path $_})] 
        [string] $BibleXMLPath,
        [string] $CompareBibleXMLPath
      )

# Load the XML file
$BibleXMLPath = Convert-Path $BibleXMLPath
$CompareBibleXMLPath = Convert-Path $CompareBibleXMLPath

$anomaliesfound = 0

[xml]$xml = Get-Content $BibleXMLPath
[xml]$compareXml = Get-Content $CompareBibleXMLPath

# Iterate through each <book> node in the Compare XML
foreach ($compareBook in $compareXml.bible.testament.book) {
    # Find the corresponding row in the CSV based on the book number
    $books = $xml.bible.testament.book
    $bookName = $compareBook.name
    $bookNumber = $compareBook.number
    $matchingBook = $books | Where-Object { $_.number -eq $bookNumber }

    # If a match is found, look for chapters
    if ($matchingBook) {
        $chapters = $compareBook.chapter
        foreach ($compareChapter in $chapters) {
            $chapterNumber = $compareChapter.number
            $matchingChapter = $matchingBook.chapter | Where-Object { $_.number -eq $chapterNumber }

            # If a matching chapter is found, check for verses
            if ($matchingChapter) {
                $verses = $compareChapter.verse
                foreach ($compareVerse in $verses) {
                    $verseNumber = $compareVerse.number
                    $matchingVerse = $matchingChapter.verse | Where-Object { $_.number -eq $verseNumber }

                    # If a matching verse is found, compare length of string.
                    if ($matchingVerse) {
                        if (($compareVerse."#text".length + 100) -le $matchingVerse."#text".length) {
                            Write-Host "Anomaly found in book: $bookName, chapter: $chapterNumber, verse: $verseNumber"
                            Write-Host "Compare text: " + $compareVerse."#text"
                            Write-Host "Matching text: " + $matchingVerse."#text"
                            $anomaliesfound++
                        }
                    }
                    else {
                        Write-Host "No matching verse found for book: $bookName, chapter: $chapterNumber, verse: $verseNumber"
                        $anomaliesfound++
                    }
                }
            }
            else{
                Write-Host "No matching chapter found for book: $bookName, chapter: $chapterNumber"
                $anomaliesfound++
            }
        }
        write-host "checked book: " + $bookName
    }
    else {
        Write-Host "No matching book found for number: $bookName"
        $anomaliesfound++
    }
}

Write-Host "Files compared: $BibleXMLPath and $CompareBibleXMLPath"
write-host "Anomalies found: $anomaliesfound"
