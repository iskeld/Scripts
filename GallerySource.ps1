<#
# Sample
function GetImgSource($node) {
    [string] $imgSrc = "http://testurl" + [ScrapySharp.Extensions.HtmlParsingHelper]::GetNextSibling($node, "a").Attributes.Item("href").Value
    return $imgSrc
}

$sourceParams = @{ "url" = "http://testurl/dummy/gallery"; "selector" = "div#mygallery div.boximage div.details a.modal-button"; "outputDir" = "output_gallery"}
#>
