﻿namespace LibGroupMe.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    /// <summary>
    /// <see cref="Message"/> represents a message in a GroupMe <see cref="Group"/> or <see cref="Chat"/>.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Message"/> class.
        /// </summary>
        internal Message()
        {
        }

        /// <summary>
        /// Gets the message identifier.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; internal set; }

        /// <summary>
        /// Gets the GUID assigned by the sender.
        /// </summary>
        [JsonProperty("source_guid")]
        public string SourceGuid { get; internal set; }

        /// <summary>
        /// Gets the identifier for the <see cref="Member"/> a Direct Message was sent to.
        /// </summary>
        [JsonProperty("recipient_id")]
        public string RecipientId { get; internal set; }

        /// <summary>
        /// Gets the Unix Timestamp when the message was created.
        /// </summary>
        [JsonProperty("created_at", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int CreatedAtUnixTime { get; internal set; }

        /// <summary>
        /// Gets the identifier for a <see cref="Member"/> who sent a Group Message.
        /// </summary>
        [JsonProperty("user_id")]
        public string UserId { get; internal set; }

        /// <summary>
        /// Gets the identifier for a <see cref="Group"/> where this message was sent.
        /// </summary>
        [JsonProperty("group_id")]
        public string GroupId { get; internal set; }

        /// <summary>
        /// Gets the name of the <see cref="Member"/> who sent the message.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; internal set; }

        /// <summary>
        /// Gets the Url of the avatar or profile picture for the <see cref="Member"/> who sent the message.
        /// </summary>
        [JsonProperty("avatar_url")]
        public string AvatarUrl { get; internal set; }

        /// <summary>
        /// Gets the message contents.
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether gets the message is a system message (GroupMe internal parameter).
        /// </summary>
        [JsonProperty("system", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool System { get; internal set; }

        /// <summary>
        /// Gets a list of identifiers for <see cref="Member"/> who 'liked' this message.
        /// </summary>
        [JsonProperty("favorited_by")]
        public IList<string> FavoritedBy { get; internal set; }

        /// <summary>
        /// Gets the type of sender who sent this message.
        /// </summary>
        [JsonProperty("sender_type")]
        public string SenderType { get; internal set; }

        /// <summary>
        /// Gets the platform this message was sent from.
        /// </summary>
        [JsonProperty("platform")]
        public string Platform { get; internal set; }

        /// <summary>
        /// Gets a list of <see cref="Attachments"/> attached to this <see cref="Message"/>.
        /// </summary>
        [JsonProperty("attachments")]
        public IList<Attachments.Attachment> Attachments { get; internal set; }

        /// <summary>
        /// Creates a new <see cref="Message"/> that can be sent to a <see cref="Group"/>.
        /// </summary>
        /// <param name="body">The message contents.</param>
        /// <param name="attachments">A list of attachments to be included with the message.</param>
        /// <returns>True if successful, false otherwise</returns>
        public static Message CreateMessage(string body, IEnumerable<Attachments.Attachment> attachments = null)
        {
            if (attachments == null)
            {
                attachments = Enumerable.Empty<Attachments.Attachment>();
            }

            var msg = new Message()
            {
                SourceGuid = Guid.NewGuid().ToString(),
                Text = body,
                Attachments = new List<Attachments.Attachment>(attachments),
            };

            return msg;
        }

        /// <summary>
        /// Likes this <see cref="Message"/>.
        /// </summary>
        {

        }
    }
}
