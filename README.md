# rx-template-recurringactionitems
Репозиторий с шаблоном разработки «Периодические поручения».

## Описание
Шаблон позволяет:
* настроить расписание и параметры рассылки периодических поручений
* автоматически формировать периодические поручения на основании расписания

Состав объектов разработки:
* Справочник «График исполнения поручения»
* Справочник «Расписание отправки поручения»
* Фоновый процесс «Отправка периодических поручений»
* Фоновый процесс «Актуализация расписания отправки периодических поручений»
* Асинхронный обработчик по созданию записей расписания отправки периодических поручений по графику
* Асинхронный обработчик по закрытию записей расписания отправки периодических поручений по графику
* Асинхронный обработчик по созданию и старту периодического поручения по расписанию

Поскольку шаблон разработки не содержит перекрытий объектов коробочного решения, конфликты при публикации не возникнут. Это позволяет использовать функциональность, как при старте нового проекта, так и в ходе сопровождения существующих инсталляций системы.

## Варианты расширения функциональности на проектах
1. Перекрытие задачи на исполнение поручения: добавление флажка «Периодическое» (только просмотр) на карточку задачи, который будет устанавливаться фоновым процессом и/или при создании графика исполнения поручения и добавление действий "Создать график" и "Показать график"

Пример названия свойства - IsPeriodic

Тип - Логическое

Пример отображаемого имени - "Периодичность"

Заполнение свойства при создании поручения:
``` C#
_obj.IsPeriodic = false;
```
Пример вычислений на кнопке "Создать график" (Выполнение):
``` C#
_obj.Save();

var schedule = DirRX.PeriodicActionItemsTemplate.PublicFunctions.Module.CreateScheduleFromActionItem(_obj);
schedule.ShowModal();

if (!schedule.State.IsInserted)
{
  _obj.IsPeriodic = true;
  _obj.Save();
}
```
Пример вычислений на кнопке "Создать график" (Возможность выполнения):
``` C#
return !_obj.State.IsChanged && 
  _obj.AccessRights.CanUpdate() &&
  DirRX.PeriodicActionItemsTemplate.RepeatSettings.AccessRights.CanCreate() &&
  !Locks.GetLockInfo(_obj).IsLockedByOther &&
  _obj.IsPeriodic != true;
```
Пример вычислений на кнопке "Показать график" (Выполнение):
``` C#
DirRX.PeriodicActionItemsTemplate.PublicFunctions.Module.ShowScheduleForActionItem(_obj);
```
Пример вычислений на кнопке "Показать график" (Возможность выполнения):
``` C#
return _obj.IsPeriodic == true;
```
Пример заполнения свойства "Периодическое" при создании периодического поручения:

Заменить данные строчки кода в серверной функции ```public virtual void StartActionItemFromScheduleItem(IScheduleItem scheduleItem)``` модуля PeriodicActionItemsTemplate
``` C#
// FIXME: На реальных проектах переделать на заполнение свойств из перекрытий.
((Sungero.Domain.Shared.IExtendedEntity)task).Params.Add("CreatedAsPeriodic", true);
```
На
``` C#
<Решение, в котором перекрыто поручение>.ActionItemExecutionTasks.As(task).IsPeriodic = true;
```

2. Перекрытие типа документа (входящее письмо, приказ, распоряжение и т.д.): добавление действий "Создать график" и "Показать график"

Пример вычислений на кнопке "Создать график" (Выполнение):
``` C#
DirRX.PeriodicActionItemsTemplate.PublicFunctions.Module.CreateScheduleFromDocument(_obj).ShowModal();
```
Пример вычислений на кнопке "Создать график" (Возможность выполнения):
``` C#
return DirRX.PeriodicActionItemsTemplate.PublicFunctions.Module.CanCreatePeriodicScheduleForDocument(_obj);
```
Пример вычислений на кнопке "Показать график" (Выполнение):
``` C#
DirRX.PeriodicActionItemsTemplate.PublicFunctions.Module.ShowSchedulesForDocument(_obj);
```
Пример вычислений на кнопке "Показать график" (Возможность выполнения):
``` C#
return DirRX.PeriodicActionItemsTemplate.PublicFunctions.Module.CanShowPeriodicScheduleForDocument(_obj);
```
3. При открытии карточки настройки периода открывать сразу вкладку «Расписание».
4. Вынесение списка настроек на обложку модуля «Делопроизводство».

## Порядок установки
Для работы требуется установленный Directum RX версии 4.7 и выше. 

### Установка для ознакомления
1. Склонировать репозиторий rx-template-recurringactionitems в папку.
2. Указать в _ConfigSettings.xml DDS:
```xml
<block name="REPOSITORIES">
  <repository folderName="Base" solutionType="Base" url="" />
  <repository folderName="RX" solutionType="Base" url="<адрес локального репозитория>" />
  <repository folderName="<Папка из п.1>" solutionType="Work" 
     url="https://github.com/DirectumCompany/rx-template-recurringactionitems" />
</block>
```

### Установка для использования на проекте
Возможные варианты:

**A. Fork репозитория**
1. Сделать fork репозитория rx-template-recurringactionitems для своей учетной записи.
2. Склонировать созданный в п. 1 репозиторий в папку.
3. Указать в _ConfigSettings.xml DDS:
``` xml
<block name="REPOSITORIES">
  <repository folderName="Base" solutionType="Base" url="" /> 
  <repository folderName="<Папка из п.2>" solutionType="Work" 
     url="<Адрес репозитория gitHub учетной записи пользователя из п. 1>" />
</block>
```

**B. Подключение на базовый слой.**

Вариант не рекомендуется, так как при выходе версии шаблона разработки не гарантируется обратная совместимость.
1. Склонировать репозиторий rx-template-recurringactionitems в папку.
2. Указать в _ConfigSettings.xml DDS:
``` xml
<block name="REPOSITORIES">
  <repository folderName="Base" solutionType="Base" url="" /> 
  <repository folderName="<Папка из п.1>" solutionType="Base" 
     url="<Адрес репозитория gitHub>" />
  <repository folderName="<Папка для рабочего слоя>" solutionType="Work" 
     url="https://github.com/DirectumCompany/rx-template-recurringactionitems" />
</block>
```

**C. Копирование репозитория в систему контроля версий.**

Рекомендуемый вариант для проектов внедрения.
1. В системе контроля версий с поддержкой git создать новый репозиторий.
2. Склонировать репозиторий rx-template-recurringactionitems в папку с ключом `--mirror`.
3. Перейти в папку из п. 2.
4. Импортировать клонированный репозиторий в систему контроля версий командой:

`git push –mirror <Адрес репозитория из п. 1>`
