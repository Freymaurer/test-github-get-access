#r "nuget: ARCtrl"

open ARCtrl

let readTxt (path: string) =
  System.IO.File.ReadAllLines(path)
  |> Array.map (fun line -> line.Split('\t'))


type AnnotationFile =
  {
    AnnotationName: string
    FilePath: string
    Organism: string
    InternalIdentifier: string
    InternalIdentifierColIndex: int
  } with
    static member make (name: string) (path: string) (organism: string) (internalIdentifier: string) (internalIdentifierColIndex: int) =
      { AnnotationName = name; FilePath = path; Organism = organism; InternalIdentifier = internalIdentifier; InternalIdentifierColIndex = internalIdentifierColIndex }

module Constants =

  module Organisms =
    let Chlamydomonas = "Chlamydomonas"
    let All = set [Chlamydomonas]

  module OntologyAnnotations =

    let InternalIdentifierMap = Map([
      Organisms.Chlamydomonas, OntologyAnnotation("cre jgi 5.5")
    ])

  [<Literal>]
  let TSVSelectorFormat = @"https://datatracker.ietf.org/doc/html/rfc7111"

  module IO =

    [<Literal>]
    let AnnotationContainerAssay = "AnnotationContainerAssay"
    let AssayDatasetPath = $"./assays/{AnnotationContainerAssay}/dataset/"
    [<Literal>]
    let AnnotationFilesFolderName = "AnnotationFiles"
    [<Literal>]
    let IDMappingFilesFolderName = "IDMappingFiles"

  module InternalIdentifiers =
    
      let idMap = Map([
        Organisms.Chlamydomonas, "cre_jgi5_5"
      ])

let arc = ARC.load(".")
let assay = arc.ISA.Value.GetAssay(Constants.IO.AnnotationContainerAssay)
let datamapRows: ResizeArray<DataContext> = ResizeArray()

open System.IO

let AnnotationFilesInfo = 
  let level1Paths = Directory.GetDirectories(Constants.IO.AssayDatasetPath)
  level1Paths 
  |> Array.collect (fun path -> 
      let organism = Path.GetFileName(path)
      if Constants.Organisms.All.Contains organism |> not then
        failwith $"Error. Missing information for Organism {organism}. Check `Constants` module!"

      let level2Paths = Directory.GetDirectories(path)
      let annotationFiles = 
        let level3Paths =
          level2Paths 
          |> Array.find (fun p -> p.EndsWith(Constants.IO.AnnotationFilesFolderName))
          |> Directory.GetFiles
        level3Paths
        |> Array.map (fun path ->
          let fileName = Path.GetFileNameWithoutExtension(path)
          let idMappingFileLines = readTxt path
          let headers = idMappingFileLines.[0]
          let internalIdentifier = Constants.InternalIdentifiers.idMap.[organism]
          let internalIdentifierIndex = 
            headers
            |> Array.findIndex (fun header -> header = internalIdentifier)
          AnnotationFile.make
            fileName
            path
            organism
            internalIdentifier
            internalIdentifierIndex
        )
      annotationFiles
  )

AnnotationFilesInfo
|> Array.iter (fun info ->
    let columnSelector = $"#col={info.InternalIdentifierColIndex+1}"
      
    let dtx = 
      DataContext(
        name = info.FilePath + columnSelector,
        format = "text/tab-separated-values",
        selectorFormat=Constants.TSVSelectorFormat,
        explication = Constants.OntologyAnnotations.InternalIdentifierMap.[info.Organism],comments = ResizeArray([
          Comment("organism", info.Organism);
          Comment("annotation type", info.AnnotationName)
          Comment("file type", "Annotation file")
        ])
      )
    datamapRows.Add dtx
)

let datamap = DataMap(datamapRows)

assay.DataMap <- Some datamap

arc.Update(".")