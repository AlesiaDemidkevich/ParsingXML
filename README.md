#  Система обработки XML-файлов (C#, .NET, RabbitMQ, SQLite)

## Описание проекта

Система состоит из двух микросервисов:

1. **FileParserService**  
   Отслеживает XML-файлы на диске, парсит их через `XmlSerializer`, изменяет состояния модулей и отправляет JSON в очередь RabbitMQ.

2. **DataProcessorService**  
   Принимает JSON-сообщения из RabbitMQ, сохраняет данные о модулях в базу данных SQLite, обновляя состояния при необходимости.

---

## Архитектура

```
FileParserService → RabbitMQ → DataProcessorService → SQLite
```

---

## Требования

- .NET 6 или выше  
- RabbitMQ  
- SQLite (встроенная, без установки)

---

## 1. Установка и запуск сервисов

### Шаг 1. Клонирование репозитория

```bash
git clone https://github.com/AlesiaDemidkevich/ParsingXML.git
cd FileParserService
```

### Шаг 2. Установка RabbitMQ

Если Docker не установлен, можно установить RabbitMQ вручную:

**Windows:**
1. Установить Erlang → https://www.erlang.org/downloads  
2. Установить RabbitMQ → https://www.rabbitmq.com/download.html  
3. После установки убедиться, что RabbitMQ запущен и доступен порт **5672**.

Проверить доступность:
```bash
telnet localhost 5672
```

---

### Шаг 3. Настройка конфигураций

#### FileParserService/appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",

  "XmlMonitor": {
    "FolderPath": "D:\\Sobes\\TestTask\\XML",
    "IntervalSeconds": 1
  },
  "RabbitMq": {
    "HostName": "localhost",
    "Port": 15672,
    "UserName": "guest",
    "Password": "guest",
    "Exchange": "xml_exchange",
    "Queue": "xml_queue",
    "RoutingKey": "xml.routing"
  }

}

```

#### DataProcessorService/appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "RabbitMq": {
    "HostName": "localhost",
    "Port": 15672,
    "UserName": "guest",
    "Password": "guest",
    "Exchange": "xml_exchange",
    "Queue": "xml_queue",
    "RoutingKey": "xml.routing"
  },
  "ConnectionStrings": {
    "Sqlite": "Data Source=data.db"
  }
}

```

> Важно: параметры `RabbitMq` должны совпадать в обоих проектах.

---

### Шаг 4. Запуск сервисов

#### Вариант 1 — через PowerShell

Откройте **два окна PowerShell**:

**Окно 1: DataProcessorService**
```bash
cd .\DataProcessorService\
dotnet run
```

**Окно 2: FileParserService**
```bash
cd .\FileParserService\
dotnet run
```

#### Вариант 2 — через Visual Studio

1. Откройте solution `FileParserService.sln`.  
2. Установите несколько стартовых проектов: FileParserService и DataProcessorService.  
3. Нажмите **F5** для запуска.

---

### Шаг 5. Проверка работы

1. Поместите XML-файл (например, `status.xml`) в папку, указанную в `FolderPath`.  
2. FileParserService через 1 секунду прочитает файл, преобразует в JSON и отправит сообщение в RabbitMQ.  
3. DataProcessorService получит сообщение и сохранит данные в SQLite.

---

## 2. Настройка RabbitMQ

Если RabbitMQ установлен локально, параметры по умолчанию:

- **HostName:** localhost  
- **Port:** 5672  
- **UserName:** guest  
- **Password:** guest  

Изменить можно в файлах `appsettings.json` в обоих сервисах.

---

## 3. Создание и инициализация БД SQLite

**DataProcessorService** автоматически создаёт базу данных при первом запуске.  
После запуска появится файл `data.db` в корне проекта.

Для проверки:

```bash
sqlite3 data.db
.tables
SELECT * FROM Modules;
```

---

## 📄 4. Формат входных XML-файлов

Пример ожидаемого файла:

```xml
<InstrumentStatus xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" schemaVersion="1.0.1">
	<PackageID>LicArt62</PackageID>
	<DeviceStatus>
		<ModuleCategoryID>SAMPLER</ModuleCategoryID>
		<IndexWithinRole>0</IndexWithinRole>
		<RapidControlStatus>&lt;?xml version="1.0" encoding="utf-16"?&gt;&lt;CombinedSamplerStatus xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema"&gt;&lt;ModuleState&gt;Online&lt;/ModuleState&gt;&lt;IsBusy&gt;false&lt;/IsBusy&gt;&lt;IsReady&gt;true&lt;/IsReady&gt;&lt;IsError&gt;false&lt;/IsError&gt;&lt;KeyLock&gt;false&lt;/KeyLock&gt;&lt;Status&gt;0&lt;/Status&gt;&lt;Vial&gt;L-A-1&lt;/Vial&gt;&lt;Volume&gt;0&lt;/Volume&gt;&lt;MaximumInjectionVolume&gt;0&lt;/MaximumInjectionVolume&gt;&lt;RackL&gt;Tray54C&lt;/RackL&gt;&lt;RackR&gt;Tray54C&lt;/RackR&gt;&lt;RackInf&gt;0&lt;/RackInf&gt;&lt;Buzzer&gt;true&lt;/Buzzer&gt;&lt;/CombinedSamplerStatus&gt;</RapidControlStatus>
	</DeviceStatus>
	<DeviceStatus>
		<ModuleCategoryID>QUATPUMP</ModuleCategoryID>
		<IndexWithinRole>0</IndexWithinRole>
		<RapidControlStatus>&lt;?xml version="1.0" encoding="utf-16"?&gt;&lt;CombinedPumpStatus xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema"&gt;&lt;ModuleState&gt;Online&lt;/ModuleState&gt;&lt;IsBusy&gt;false&lt;/IsBusy&gt;&lt;IsReady&gt;true&lt;/IsReady&gt;&lt;IsError&gt;false&lt;/IsError&gt;&lt;KeyLock&gt;false&lt;/KeyLock&gt;&lt;Mode&gt;ISO&lt;/Mode&gt;&lt;Flow&gt;0&lt;/Flow&gt;&lt;PercentB&gt;0&lt;/PercentB&gt;&lt;PercentC&gt;0&lt;/PercentC&gt;&lt;PercentD&gt;0&lt;/PercentD&gt;&lt;MinimumPressureLimit&gt;0&lt;/MinimumPressureLimit&gt;&lt;MaximumPressureLimit&gt;400.0330947751624&lt;/MaximumPressureLimit&gt;&lt;Pressure&gt;0&lt;/Pressure&gt;&lt;PumpOn&gt;false&lt;/PumpOn&gt;&lt;Channel&gt;0&lt;/Channel&gt;&lt;/CombinedPumpStatus&gt;</RapidControlStatus>
	</DeviceStatus>
	<DeviceStatus>
		<ModuleCategoryID>COLCOMP</ModuleCategoryID>
		<IndexWithinRole>0</IndexWithinRole>
		<RapidControlStatus>&lt;?xml version="1.0" encoding="utf-16"?&gt;&lt;CombinedOvenStatus xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema"&gt;&lt;ModuleState&gt;Online&lt;/ModuleState&gt;&lt;IsBusy&gt;false&lt;/IsBusy&gt;&lt;IsReady&gt;true&lt;/IsReady&gt;&lt;IsError&gt;false&lt;/IsError&gt;&lt;KeyLock&gt;false&lt;/KeyLock&gt;&lt;UseTemperatureControl&gt;false&lt;/UseTemperatureControl&gt;&lt;OvenOn&gt;false&lt;/OvenOn&gt;&lt;Temperature_Actual&gt;26.22&lt;/Temperature_Actual&gt;&lt;Temperature_Room&gt;27.81&lt;/Temperature_Room&gt;&lt;MaximumTemperatureLimit&gt;0&lt;/MaximumTemperatureLimit&gt;&lt;Valve_Position&gt;0&lt;/Valve_Position&gt;&lt;Valve_Rotations&gt;0&lt;/Valve_Rotations&gt;&lt;Buzzer&gt;true&lt;/Buzzer&gt;&lt;/CombinedOvenStatus&gt;</RapidControlStatus>
	</DeviceStatus>
</InstrumentStatus>
```

Файл читается через `XmlSerializer`.  
Вложенный `RapidControlStatus` десериализуется как отдельный XML-блок (`CombinedSamplerStatus`, `CombinedPumpStatus`, `CombinedOvenStatus` и др.).

---

## 5. Логирование и обработка ошибок

- Все ключевые события логируются через `ILogger`.  
- Ошибки при парсинге или недоступности RabbitMQ не прерывают работу сервисов.  
- Логи выводятся в консоль и при необходимости сохраняются в файлы.

---

## 6. Полезные команды

Очистить очередь RabbitMQ:
```bash
rabbitmqctl purge_queue xml_queue
```

Проверить содержимое SQLite:
```bash
sqlite3 data.db
SELECT * FROM Modules;
```

---

## Результат

После успешного запуска:
- **FileParserService** читает XML-файлы и публикует JSON в RabbitMQ.  
- **DataProcessorService** принимает JSON и сохраняет данные в SQLite.  
- В БД `data.db` таблица `Modules` содержит:
  - ModuleCategoryID  
  - ModuleState  
  - Timestamp
