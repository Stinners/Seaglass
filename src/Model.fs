namespace Seaglass 

module Model = 

    type FSFile = 
        | File
        | Directory of expanded : bool

        
    type FSRecord = 
        { root: FSFile
          path: string
          contents: List<FSRecord>
          isFocused: bool }


    type ViewType = 
        | Note  
        | Search 
        | Help


    type FileTree = 
        { filesystem: FSRecord
          size: int
          isOpen: bool 
          isFocused: bool }


    type OpenNote = 
        { name: string
          path: string 
          text: string } 


    type Model = 
        { fileTree: FileTree
          view: ViewType
          shutdown: bool 
          note: OpenNote }
