open System 
open System.IO

let filePath = "./chlamy_jgi55.txt"

let fileRows = 
    File.ReadAllLines(filePath)
    |> Array.skip 2 // Skip the first two header lines
    |> Array.map (fun line -> 
        let parts = line.Split('\t')
        {|
          creId = parts.[0].Trim()
          MapManId = parts.[1].Trim()
          MapManDescription = parts.[2].Trim()
          Localization = parts.[3].Trim()
          GOId = parts.[4].Trim()
          GODescription = parts.[5].Trim()
          Synonym = parts.[6].Trim()
        |}
      )

let outputBasePath = "./assays/AnnotationContainerAssay/dataset/Chlamydomonas/AnnotationFiles"

let writeAnnotationFile<'A> (dataRows: 'A []) (collector: 'A -> string []) (headerRow: string []) (fileName: string) =
  let filePath = Path.Combine(outputBasePath, fileName)
  let sb = new System.Text.StringBuilder()
  sb.AppendLine(String.Join("\t", headerRow)) |> ignore
  dataRows
  |> Array.map (collector >> String.concat "\t")
  |> Array.iter (fun line -> sb.AppendLine(line) |> ignore)
  let fileContent = sb.ToString()
  File.WriteAllText(filePath, fileContent)

let InternalChlamyId = "cre_jgi5_5"

writeAnnotationFile //mapman
  fileRows 
  (fun row -> [| row.creId; row.MapManId; row.MapManDescription |]) 
  [| InternalChlamyId; "MapManId"; "MapManDescription" |] 
  "MapMan.tsv"

writeAnnotationFile //Localization
  fileRows 
  (fun row -> [| row.creId; row.Localization |]) 
  [| InternalChlamyId; "Localization" |] 
  "Localization.tsv"

writeAnnotationFile //GO
  fileRows 
  (fun row -> [| row.creId; row.GOId; row.GODescription |]) 
  [| InternalChlamyId; "GO"; "GODescription" |] 
  "GO.tsv"
  
writeAnnotationFile //Synonym
  fileRows 
  (fun row -> [| row.creId; row.Synonym |]) 
  [| InternalChlamyId; "Synonym" |] 
  "Synonym.tsv"