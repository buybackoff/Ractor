
open System


type Base() =
    
    abstract OnDispose: unit -> unit
    default this.OnDispose() = Console.WriteLine("Base on dispose")

    interface IDisposable with
        member this.Dispose() = this.OnDispose()


type Derived() =
    inherit Base()

    override this.OnDispose() = () //Console.WriteLine("Derived on dispose")

    interface IDisposable with
        member this.Dispose() = Console.WriteLine("Derived disposable")

do
    use d = 
        { new Derived() with
                override this.OnDispose() = Console.WriteLine("OE on d") 
          interface IDisposable with
                member __.Dispose() = Console.WriteLine("OE Disposable")
                }

    ()

type State = { intList : int list }

let a = { intList = [1; 2; 3; 4] }

let b = {a with intList = a.intList @ [5]}