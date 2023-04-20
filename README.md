# Microservice runtime draft


## Concept 
* Microservices are bound to a subdomain transaction
* Each microservice use a persistence object to save entities and events
* A runtime generic host read events and dispatch them into Azure Service Bus Queue

Reference-style:
![alt_text][concept]

[concept]: https://learningruntimestor.blob.core.windows.net/runtimedocumentation/MicroserviceConcept.png "Microservice concept"

