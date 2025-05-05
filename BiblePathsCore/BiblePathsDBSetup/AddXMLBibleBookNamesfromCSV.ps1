Param(
        [ValidateScript({Test-Path $_})] 
        [string] $xmlFilePath,
        [string] $csvFilePath

      )

# Load the XML file
$BibleXMLPath = Convert-Path $xmlFilePath
[xml]$xml = Get-Content $BibleXMLPath

# Load the CSV file
$bookNames = Import-Csv $csvFilePath

# Iterate through each <book> node in the XML
foreach ($book in $xml.bible.testament.book) {
    # Find the corresponding row in the CSV based on the book number
    $bookNumber = $book.number
    $matchingRow = $bookNames | Where-Object { $_."Book Number" -eq $bookNumber }

    # If a match is found, add the name attribute
    if ($matchingRow) {
        write-host "Adding book: " + $matchingRow.Name
        $book.SetAttribute("name", $matchingRow.Name)
    }
}

# Save the updated XML back to the file
$xml.Save($BibleXMLPath)

Write-Host "The name attributes have been added to the book nodes in the XML file."
