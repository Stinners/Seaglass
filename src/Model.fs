namespace Seaglass 

open System.IO

module Model = 
    type FSDirectory = 
        { directory: Directory
          expanded: bool }


    type FsFile = 
        | File of File
        | Directory of Directory * List<FsFile> 


    type FSRecord = 
        { content: List<FSRecord>
          isFocused: bool }


    // Search and Help will both take over the whole UI when they're focused
    type PaneType = 
        | Note of fileTreeFocus : bool 
        | Search 
        | Help


    type FileTree = 
        { filesystem: FSRecord
          size: int
          isOpen: bool }


    type Model = 
        { fileTree: FileTree
          view: PaneType
          shutdown: bool }

    //=================== Initialization ========================//
    
    let initFileSystem = 
        { content = [] 
          isFocused = true }


    let initFileTree = 
        { filesystem = initFileSystem 
          size = 40
          isOpen = true }


    let initModel = 
        { view = Note false
          fileTree = initFileTree
          shutdown = false }
