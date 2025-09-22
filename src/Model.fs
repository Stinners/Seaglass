namespace Seaglass 

open Spectre.Console.Rendering

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
          text: IRenderable array
          scroll: int }

    type Command = Editor

    type Model = 
        { filetree: FileTree
          view: ViewType
          shutdown: bool 
          note: OpenNote
          command : Option<Command>
        }
