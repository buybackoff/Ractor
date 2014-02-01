(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"


(**
Fredis
===================

Documentation

<div class="row">
  <div class="span1"></div>
  <div class="span6">
    <div class="well well-small" id="nuget">
      The Fredis library can be <a href="https://nuget.org/packages/Fredis">installed from NuGet</a>:
      <pre>PM> Install-Package Fredis -Pre</pre>
    </div>
  </div>
  <div class="span1"></div>
</div>

Example
-------

This example demonstrates using a function defined in this sample library.

*)

#r "BookSleeve.dll"
#r "Fredis.dll"
open System
open System.Text

open Fredis

let connection = new Connection("localhost")
let anotherReusedConnection = +connection
let subscriberChannel = %connection
let r1 = !!!connection.Server.Ping()
!~!connection.Strings.Set(2,"k1","1")
let a' = 
    async{
        // Async.AwaitIAsyncResult >> Async.Ignore
        return! !~connection.Strings.Set(1,"k1","abc")
    } |> Async.StartImmediate
// !~! shortcut for (Async.AwaitIAsyncResult >> Async.Ignore >> Async.StartImmediate)
let a = !~!connection.Strings.Set(1,"k1","abc") 
let r2 = async { return! !!connection.Strings.Append(1,"k1","def") } |> Async.RunSynchronously
let r3 = !!!connection.Strings.GetString(1,"k1")

(**
Some more info

Samples & documentation
-----------------------

TDB
 
Contributing and copyright
--------------------------

The project is hosted on [GitHub][gh] where you can [report issues][issues], fork 
the project and submit pull requests. If you're adding new public API, please also 
consider adding [samples][content] that can be turned into a documentation. You might
also want to read [library design notes][readme] to understand how it works.

(c) Victor Baybekov 2014

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

This software is distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

BookSleeve is licensed separately; see https://code.google.com/p/booksleeve/.
Redis is licensed separately; see http://redis.io/.


  [content]: https://github.com/buybackoff/Fredis/tree/master/docs/content
  [gh]: https://github.com/buybackoff/Fredis
  [issues]: https://github.com/buybackoff/Fredis/issues
  [readme]: https://github.com/buybackoff/Fredis/blob/master/README.md
  [license]: https://github.com/buybackoff/Fredis/blob/master/LICENSE.txt
*)
