# HelloWorld Sample



## Purpose

This sample illustrates how to use the NuGet packages to build and deploy.

It does NOT show the capabilities of the programming model. Check the samples in the Application folder for to find out more about the various features of Reactive Machine.


## Description of Projects

HelloWorld.Service:

	defines the application logic. For this HelloWorld example the service contains a single orchestration that writes "Hello World" to the log.


HelloWorld.Test:

	defines a service that, on startup, calls the HelloWorld service a specified number of times.


HelloWorld.Test.OnEmulator:

	an emulator deployment of the HelloWorld test.


HelloWorld.Test.OnFunctions:

	an Azure Functions deployment of the HelloWorld test.


NOTE: this is a preliminary version of the sample.
The intention is to host the HelloWorld service in Azure functions using an Http binding.