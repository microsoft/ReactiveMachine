
# Reactive Machine How-To

## Getting Started

All code can be compiled from the Visual Studio Solution All.sln.

- *ReactiveMachine.Abstractions* contains the interfaces and attributes for defining orchestrations, activities, state, affinities, and events. It is required for defining the application logic of a microservice.

- *ReactiveMachine.Compiler* contains the code for compiling the application logic into a deterministic state machine suitable for hosting on a back-end.


(TODO write more here)

 
## Build and Test

The solution All.sln builds all the code. The application folder contains samples. The projects ending in *.OnEmulator can be run locally. The projects ending in *.OnFunctions can be run on Azure Functions (either on the local emulator or deployed to the Azure portal).

Some environment variables eed to be set. How to set them depends on the host used. For the functions host, environment variables can be set directly in the portal.

| Environment Variable       | Hosts   | Meaning |
|----------------------------|---------|---------|
| REACTIVE_MACHINE_DIR       | all     | Set it to the repository path on your development machine, and leave it blank in the cloud. This allows the (optional) telemetry tools to detect cloud vs. local, and launch interactive tools in the local development setting.  |
| REACTIVE_MACHINE_TELEMETRY | all     | An azure storage connection string for telemetry data (configuration information, events, statistics). Data is stored as JSON in blobs. Defaults to development storage. |
| EVENTHUBS_CONNECTION_STRING | functions | The event hubs connection string. |
| APPINSIGHTS_INSTRUMENTATIONKEY | functions | (optional) An AppInsights instrumentation key to use for generating telemetry. |


