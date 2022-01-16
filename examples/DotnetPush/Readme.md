# DotNet Push 

DotNet push is built for testing Push notifications. It can be used as a starting point for new projects as it shows how the Ably sdk is setup for push notification.

## Setup

To setup the projects you need to get setup for push notifications. Read the `Push_README.md` in the root of the repository first. 

For Android you need to use an emulator with Google Services enabled or a physical device, for Apple you need to have a physical device as Push notifications do not work on the emulator. 

## The App

Now everything is setup, here is what you can do: 

1. Activate / Deactivate Push notifications for the device. 
2. View / Subscribe for push notifications on specific channels. The Channels screen does not subscribe for normal Ably messages but only for Push notifications. You can also see what channels is the current device subscribed to. 
3. Notification: This screen shows all the Push notification received by the app. It helps you debug what data is being sent and received on the device. 
4. State: The state screen shows the internal state of the Push notification implementation. It is there to help debug the implementation. 
5. Logs: Shows the Ably SDK internal log.
