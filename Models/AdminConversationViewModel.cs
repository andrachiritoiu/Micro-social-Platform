using System;

namespace MicroSocialPlatform.Models
{
    public class AdminConversationViewModel
    {
        public ApplicationUser User1 { get; set; }
        public ApplicationUser User2 { get; set; }
        public Message LastMessage { get; set; }
        public int MessageCount { get; set; }
    }
}
