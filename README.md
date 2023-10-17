## Notes
- There have three type of channel (Regular, QUeue Group, Streaming)
- Regular: Pub/Sub methology, which mean client receive message in time if publisher publish anything. `Be note the data isn't persistence`
- Queue Group: Kind of load balancer which distribute the data (randomly) to each client. Example, 10 messages are ready to publish, and client 1 receive 4 and client 2 receive other 6.
- Streaming: The data is persistence, which client are able review back the missing data
  - DurableName : Each client must have different durable name for nat streaming so nats are only fetching the data the client havent seen.
