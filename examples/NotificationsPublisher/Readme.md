# Notification publisher

The notification publisher is a small cross platform console app to help with publishing test Ably messages. It was initially built to test Push notifications. 

Requirements: 
- .Net 5.0 SDK 

## Running the app

You can start the app using `dotnet run` 

## Functionality 

When you run the app for the first time you will be asked to enter an Ably `key` which will be saved in a file called `key.secret` in the current directory. 
Once the key is entered and stored the app can: 
- Connect to Ably
- Disconnect 
- View Ably logs
- Send messages to a channel

### Sending

When sending a message you will be asked to enter a `Channel` name, `name` of the message, `data` for the message and there is an extra field for message extras. Message extras are usually sent in json format. 

