﻿using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using RapidCMS.Common.Data;
using RapidCMS.Common.Enums;
using RapidCMS.Common.Forms;
using RapidCMS.Common.Models.Metadata;

namespace RapidCMS.UI.Components.Editors
{
    public class BaseEditor : EditContextComponentBase
    {
        protected ValidationState State { get; private set; }

        [Parameter] internal protected IEntity Entity { get; private set; }

        [Parameter] internal protected IPropertyMetadata Property { get; private set; }

        protected object GetValueAsObject()
        {
            return Property.Getter(Entity);
        }

        protected string GetValueAsString()
        {
            return TypeDescriptor.GetConverter(Property.PropertyType).ConvertToString(GetValueAsObject());
        }

        protected IEnumerable<string> GetValidationMessages()
        {
            return EditContext.GetValidationMessages(Property);
        }

        private void ValidationStateChangeHandler(object sender, ValidationStateChangedEventArgs eventArgs)
        {
            if (EditContext.WasValidated(Property))
            {
                if (EditContext.IsValid(Property))
                {
                    State = ValidationState.Valid;
                }
                else
                {
                    State = ValidationState.Invalid;
                }
            }
            else
            {
                State = ValidationState.NotValidated;
            }

            StateHasChanged();
        }

        protected override void AttachValidationStateChangedListener()
        {
            if (EditContext != null)
            {
                EditContext.OnValidationStateChanged += ValidationStateChangeHandler;
                EditContext.NotifyPropertyStartedListening(Property);

                State = ValidationState.NotValidated;
            }
        }

        protected override void DetachValidationStateChangedListener()
        {
            if (EditContext != null)
            {
                EditContext.OnValidationStateChanged -= ValidationStateChangeHandler;
            }
        }
    }
}
