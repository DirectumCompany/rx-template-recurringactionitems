using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.IncomingLetter;

namespace DirRX.Solution.Shared
{
	partial class IncomingLetterFunctions
	{
		/// <summary>
		/// Изменить отображение панели регистрации.
		/// </summary>
		/// <param name="needShow">Признак отображения.</param>
		/// <param name="repeatRegister">Признак повторной регистрации\изменения реквизитов.</param>
		public override void ChangeRegistrationPaneVisibility(bool needShow, bool repeatRegister)
		{
			base.ChangeRegistrationPaneVisibility(needShow, repeatRegister);
			
			var properties = _obj.State.Properties;
			properties.PageCount.IsEnabled = needShow;
			properties.PageCount.IsVisible = needShow;
		}
		
		/// <summary>
		/// Сменить доступность реквизитов документа, блокируемых после регистрации.
		/// </summary>
		/// <param name="isEnabled">True, если свойства должны быть доступны.</param>
		/// <param name="isRepeatRegister">True, если повторная регистрация.</param>
		public override void ChangeDocumentPropertiesAccess(bool isEnabled, bool isRepeatRegister)
		{
			base.ChangeDocumentPropertiesAccess(isEnabled, isRepeatRegister);

			_obj.State.Properties.Assignee.IsEnabled = false;
		}
		
		/// <summary>
		/// Автозаполнение имени.
		/// </summary>
		public override void FillName()
		{
			
			if (_obj.DocumentKind != null && _obj.Correspondent != null && !string.IsNullOrEmpty(_obj.Subject))
			{
				var name = string.Empty;
				var from = IncomingLetters.Resources.From;
				
				using (TenantInfo.Culture.SwitchTo())
				{
					name = string.Format("{0} {1} {2}", _obj.DocumentKind.ShortName, from, _obj.Correspondent.DisplayValue);
					
					if (_obj.SignedBy != null)
						name += string.Format(" {0} {1}", from, CaseConverter.ConvertPersonFullNameToTargetDeclension(CaseConverter.SplitPersonFullName(_obj.SignedBy.Name), DeclensionCase.Genitive));
					
					name += string.Format(" \"{0}\"", _obj.Subject);
					
					if (_obj.RegistrationState == RegistrationState.Registered)
						name += string.Format(", {0}{1} {2} {3}", IncomingLetters.Resources.Number, _obj.RegistrationNumber, from, _obj.RegistrationDate.Value.ToString("d"));
					
					_obj.Name = name.Length > 250 ? name.Substring(0, 250) : name;
				}
			}
			else
				_obj.Name = IncomingLetters.Resources.DocumentNameAutotext;
			
		}
	}
}