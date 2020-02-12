using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiblePathsCore.Models.DB
{
    public partial class Paths
    {
        public void SetInitialProperties(string OwnerEmail)
        {
            // Set key properties that will not be supplied by the user on creation
            Owner = OwnerEmail;
            Length = 0;
            ComputedRating = (decimal)4.5; // this is just a default it will change.
            Created = DateTime.Now;
            Modified = DateTime.Now;
            Topics = "";
            IsPublished = false;
            IsDeleted = false;
            StepCount = 0;
            Reads = 0;
        }

        public bool IsValidPathEditor(string UserEmail)
        {
            if (string.IsNullOrEmpty(UserEmail))
            {
                return false;
            }
            if (IsPublicEditable || Owner.ToLower() == UserEmail.ToLower()) 
            { 
                return true; 
            }
            // worst case we'll return false 
            return false; 
        }

        public bool IsPathOwner(string UserEmail)
        {
            if (Owner.ToLower() == UserEmail.ToLower())
            {
                return true; 
            }
            else
            {
                return false;
            }
        }
    }
}
