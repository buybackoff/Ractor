(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"


(**
Fredis
===================

Install
------------------
<div class="row">
  <div class="span1"></div>
  <div class="span6">
    <div class="well well-small" id="nuget">
      The Fredis library can be <a href="https://nuget.org/packages/Fredis">installed from NuGet</a>:
      <pre>PM> Install-Package Fredis</pre>
    </div>
  </div>
  <div class="span1"></div>
</div>

Usage Example
-------

This example demonstrates using a function defined in this sample library.

*)

#r "Fredis.dll"
#r "Fredis.Persistence.dll"

open System
open System.Text

open Fredis


let fredis = new Fredis("localhost:6379")

let computation (input:string) : Async<string> =
    Console.WriteLine("Hello, " + input)
    async {
        return "Hello, " + input
    }

let firstGreeter = fredis.CreateActor("greeter", computation)
//let sameGreeter  = fredis.GetActor<string, string>("greeter")
firstGreeter.Start()

firstGreeter.Post("First Greeter")
//sameGreeter.Post("Same Greeter")
//"greeter" <-- "Greeter Using Operator"



(**
 
Contributing and copyright
--------------------------

The project is hosted on [GitHub][gh] where you can [report issues][issues], fork 
the project and submit pull requests. If you're adding new public API, please also 
consider adding [samples][content] that can be turned into a documentation. You might
also want to read [library design notes][readme] to understand how it works.


(c) Victor Baybekov 2014.
Licensed under the Apache License, Version 2.0 (the "License")


  [content]: https://github.com/buybackoff/Fredis/tree/master/docs/content
  [gh]: https://github.com/buybackoff/Fredis
  [issues]: https://github.com/buybackoff/Fredis/issues
  [readme]: https://github.com/buybackoff/Fredis/blob/master/README.md
  [license]: https://github.com/buybackoff/Fredis/blob/master/LICENSE.txt
*)
