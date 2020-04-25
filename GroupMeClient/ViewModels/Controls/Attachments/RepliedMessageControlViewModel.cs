﻿using GalaSoft.MvvmLight;
using GroupMeClient.Caching;
using GroupMeClientApi.Models;

namespace GroupMeClient.ViewModels.Controls.Attachments
{
    /// <summary>
    /// <see cref="RepliedMessageControlViewModel"/> provides a ViewModel for the <see cref="Views.Controls.Attachments.RepliedMessageControl"/> control.
    /// </summary>
    public class RepliedMessageControlViewModel : AttachmentViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RepliedMessageControlViewModel"/> class.
        /// </summary>
        /// <param name="originalMessageId">The message id of the original message that is being replied to.</param>
        /// <param name="messageContainer">The message container in which the original message is contained.</param>
        /// <param name="cacheContext">The caching context in which messages are stored.</param>
        /// <param name="nestLevel">The number of attachment deeply nested this <see cref="Message"/> is.</param>
        public RepliedMessageControlViewModel(string originalMessageId, IMessageContainer messageContainer, CacheContext cacheContext, int nestLevel)
        {
            var originalMessage = cacheContext.Messages.Find(originalMessageId);
            if (originalMessage == null)
            {
                // problem
            }
            else
            {
                if (messageContainer is Group g)
                {
                    originalMessage.AssociateWithGroup(g);
                }
                else if (messageContainer is Chat c)
                {
                    originalMessage.AssociateWithChat(c);
                }

                this.Message = new MessageControlViewModel(originalMessage, cacheContext, false, true, nestLevel + 1);
            }
        }

        /// <inheritdoc/>
        public override bool Shrinkable => false;

        /// <summary>
        /// Gets the original <see cref="MessageControlViewModel"/> containing the <see cref="Message"/> that is being replied to.
        /// </summary>
        public MessageControlViewModel Message { get; }
    }
}
