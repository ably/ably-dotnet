using IO.Ably.MessageEncoders;
using System;
using System.Collections.Generic;
using System.Text;

namespace IO.Ably.Tests.Shared.Extensions
{
    internal static class MessageExtensions
    {
        /// <summary>
        /// Decodes the json representation of a Message using the default list of encoders.
        /// </summary>
        /// <param name="messageJson">json representation of a Message.</param>
        /// <param name="options">optional channel options. <see cref="ChannelOptions"/>.</param>
        /// <returns>message with decoded payload.</returns>
        /// <exception cref="AblyException">AblyException if there is an issue decoding the message. The most likely error is invalid json string.</exception>
        public static Message FromEncoded(string messageJson, ChannelOptions options = null)
        {
            try
            {
                var message = JsonHelper.Deserialize<Message>(messageJson);
                return FromEncoded(message, options);
            }
            catch (Exception e)
            {
                DefaultLogger.Error($"Error decoding message: {messageJson}", e);
                throw new AblyException("Error decoding message. Error: " + e.Message, ErrorCodes.InternalError);
            }
        }

        /// <summary>
        /// Decodes a json representation of an array of messages using the default list of encoders.
        /// </summary>
        /// <param name="messagesJson">json representation of an array of messages.</param>
        /// <param name="options">optional channel options. <see cref="ChannelOptions"/>.</param>
        /// <returns>array of decoded messages.</returns>
        /// <exception cref="AblyException">AblyException if there is an issue decoding the message. The most likely error is invalid json string.</exception>
        public static Message[] FromEncodedArray(string messagesJson, ChannelOptions options = null)
        {
            try
            {
                var messages = JsonHelper.Deserialize<List<Message>>(messagesJson).ToArray();
                return FromEncodedArray(messages, options);
            }
            catch (Exception e)
            {
                DefaultLogger.Error($"Error decoding message: {messagesJson}", e);
                throw new AblyException("Error decoding messages. Error: " + e.Message, ErrorCodes.InternalError);
            }
        }

        /// <summary>
        /// Decodes the current message data using the default list of encoders.
        /// </summary>
        /// <param name="encoded">encoded message object.</param>
        /// <param name="options">optional channel options. <see cref="ChannelOptions"/>.</param>
        /// <returns>message with decoded payload.</returns>
        public static Message FromEncoded(Message encoded, ChannelOptions options = null)
        {
            return FromEncodedHandler(encoded, options);
        }

        /// <summary>
        /// Decodes an array of messages. <see cref="FromEncoded(Message, ChannelOptions)"/>.
        /// </summary>
        /// <param name="encoded">array of encoded Messages.</param>
        /// <param name="options">optional channel options. <see cref="ChannelOptions"/>.</param>
        /// <returns>array of decoded messages.</returns>
        public static Message[] FromEncodedArray(Message[] encoded, ChannelOptions options = null)
        {
            return FromEncodedArrayHandler(encoded, options);
        }

        internal static T FromEncodedHandler<T>(T encoded, ChannelOptions options = null)
            where T : IMessage
        {
            var context = options.ToDecodingContext(DefaultLogger.LoggerInstance);
            var result = MessageHandler.DecodePayload(encoded, context, logger: DefaultLogger.LoggerInstance);
            if (result.IsFailure)
            {
                throw new AblyException(result.Error);
            }

            return encoded;
        }

        internal static T[] FromEncodedArrayHandler<T>(T[] encodedArray, ChannelOptions options = null)
            where T : IMessage
        {
            var context = options.ToDecodingContext(DefaultLogger.LoggerInstance);
            foreach (var encoded in encodedArray)
            {
               MessageHandler.DecodePayload(encoded, context, logger: DefaultLogger.LoggerInstance);
            }

            return encodedArray;
        }
    }
}
