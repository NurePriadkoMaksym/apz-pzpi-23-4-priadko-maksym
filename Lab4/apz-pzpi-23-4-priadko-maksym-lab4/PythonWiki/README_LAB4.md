# Лабораторна робота 4 — масштабування бекенда PythonWiki

## Що реалізовано

У проєкт додано демонстрацію **горизонтального масштабування бекенда в Kubernetes** без Python-інсталятора:

- `PythonWiki API` запускається як Kubernetes `Deployment` і масштабується на 1, 2, 3+ pod-и.
- `SQL Server` запускається окремим deployment-ом із persistent volume.
- `Service` типу `LoadBalancer` розподіляє HTTP-запити між API pod-ами.
- Додано endpoint `/health`, який повертає назву pod-а / контейнера в полі `machine`.
- Додано автоматизований PowerShell-інсталятор `deploy/Install-Lab4.ps1`.
- Додано простий PowerShell dashboard `deploy/Dashboard-Lab4.ps1`.
- Додано PowerShell-навантажувальний тест `loadtest/Invoke-LoadTest.ps1`.
- Додано `.bat`-обгортки для зручного запуску на Windows.

Це відповідає варіанту: **горизонтальне масштабування сервера**.

---

## Передумови

Потрібно мати:

- Docker Desktop;
- увімкнений Kubernetes у Docker Desktop;
- `kubectl` у PATH;
- PowerShell.

Python більше не потрібен для інсталяції, dashboard або базового load test.

---

## Швидкий запуск

Виконувати команди з папки `PythonWiki`.

Найпростіший запуск на Windows:

```bat
.\deploy\install.bat
```

Або напряму через PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\Install-Lab4.ps1
```

Інсталятор автоматично:

1. перевіряє наявність `docker` і `kubectl`;
2. читає параметри з `deploy/config.env`;
3. збирає Docker image бекенда з `--no-cache`, щоб Kubernetes не взяв старий image без `/health`;
4. генерує Kubernetes manifests у `k8s/generated/`;
5. видаляє старий API deployment, якщо він був, і виконує `kubectl apply`;
6. запускає API спочатку в 1 replica, щоб EF migrations не виконувалися паралельно;
7. після успішного старту масштабує API до значення `API_REPLICAS`;
8. показує pod-и, service-и й PVC;
9. робить smoke test через `/health`.

Після запуску API має бути доступне за адресами:

```text
http://localhost:8080/health
http://localhost:8080/swagger
```

---

## Конфігурація інсталятора

Параметри знаходяться у файлі:

```text
deploy/config.env
```

Приклад:

```env
NAMESPACE=pythonwiki
API_IMAGE=pythonwiki-api:lab4
API_REPLICAS=2
API_PORT=8080
DB_NAME=PythonWikiDb
DB_PASSWORD=YourStrong!Passw0rd
STORAGE_SIZE=2Gi
BUILD_IMAGE=true
NO_CACHE_BUILD=true
RECREATE_NAMESPACE=false
```

Тобто для зміни кількості replicas, порту, назви namespace або пароля БД не треба редагувати Kubernetes yaml — достатньо змінити `config.env` і знову запустити інсталятор.

Для повністю неінтерактивного запуску:

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\Install-Lab4.ps1 -NonInteractive
```

Для чистого перевстановлення з видаленням namespace:

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\Install-Lab4.ps1 -Recreate
```

Увага: `-Recreate` видаляє namespace і PVC бази даних.

---

## Якщо API зависає на `0/1 Ready`

Найчастіші причини:

1. Kubernetes використовує старий Docker image із тим самим тегом. У старому image може не бути endpoint `/health`, тому probe повертає `404`.
2. Кілька API replicas одночасно запускають EF migrations. Через це один pod може отримати помилку `Database already exists`.

В оновленому інсталяторі це виправлено: image збирається з `--no-cache`, старий API deployment видаляється перед повторним deploy, API стартує спочатку з 1 replica, а після міграцій масштабується до `API_REPLICAS`.

Для чистого повторного запуску можна виконати:

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\Install-Lab4.ps1 -Recreate -NonInteractive
```

Увага: `-Recreate` видаляє namespace і PVC бази даних.

## Керування через dashboard

Запуск:

```bat
.\deploy\dashboard.bat
```

Або:

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\Dashboard-Lab4.ps1
```

Dashboard дозволяє:

1. показати pod-и;
2. показати service-и та PVC;
3. змінити кількість API replicas;
4. показати логи API;
5. зробити серію запитів до `/health`;
6. запустити PowerShell load test;
7. відкрити Swagger.

---

## Ручні команди для демонстрації

Перевірити pod-и:

```powershell
kubectl get pods -n pythonwiki -o wide
```

Збільшити кількість API pod-ів до 3:

```powershell
kubectl scale deployment/pythonwiki-api --replicas=3 -n pythonwiki
```

Дочекатися rollout:

```powershell
kubectl rollout status deployment/pythonwiki-api -n pythonwiki
```

Зменшити до 1 pod-а:

```powershell
kubectl scale deployment/pythonwiki-api --replicas=1 -n pythonwiki
```

Перевірити сервіс:

```powershell
Invoke-RestMethod http://localhost:8080/health
```

При кількох pod-ах поле `machine` може змінюватися, що демонструє load balancing.

---

## Навантажувальне тестування без Python

Через dashboard обрати пункт:

```text
6. Run PowerShell load test
```

Або напряму:

```powershell
powershell -ExecutionPolicy Bypass -File .\loadtest\Invoke-LoadTest.ps1 -Url http://localhost:8080/health -Users 50 -RequestsPerUser 100
```

Скрипт виконує паралельні HTTP-запити й рахує:

- total requests;
- successful requests;
- failed requests;
- duration;
- requests per second;
- average latency;
- p95 latency;
- p99 latency.

Результат також зберігається у CSV-файл у папці `loadtest`.

Рекомендований сценарій для звіту:

1. Встановити `replicas=1`, запустити load test, записати RPS, latency, errors.
2. Встановити `replicas=2`, повторити тест.
3. Встановити `replicas=3`, повторити тест.
4. Порівняти результати.

Приклад таблиці для звіту:

| Replicas | Users | Requests per user | RPS | Avg latency | P95 latency | Errors |
|---:|---:|---:|---:|---:|---:|---:|
| 1 | 50 | 100 | заповнити після тесту | заповнити після тесту | заповнити після тесту | заповнити після тесту |
| 2 | 50 | 100 | заповнити після тесту | заповнити після тесту | заповнити після тесту | заповнити після тесту |
| 3 | 50 | 100 | заповнити після тесту | заповнити після тесту | заповнити після тесту | заповнити після тесту |

---

## Що показати викладачу

1. `deploy/config.env` — текстовий конфігураційний файл.
2. `deploy/Install-Lab4.ps1` — інсталятор, який сам генерує Kubernetes manifests і розгортає систему.
3. `k8s/generated/` — manifests, створені інсталятором.
4. `deploy/Dashboard-Lab4.ps1` — панель керування масштабуванням.
5. `kubectl get pods -n pythonwiki` до і після масштабування.
6. `/health` — API працює та показує pod/container name.
7. Результати load test для 1, 2, 3 replicas.

Формулювання для захисту:

> У роботі реалізовано горизонтальне масштабування backend API у Kubernetes. Інсталятор на PowerShell читає конфігураційний файл, генерує Kubernetes manifests, збирає Docker image і розгортає API та SQL Server у кластері. Dashboard дозволяє керувати кількістю replicas, переглядати pod-и, логи й запускати навантажувальне тестування. Kubernetes Service виконує load balancing між API pod-ами.

---

## Видалення розгортання

```bat
.\deploy\uninstall.bat
```

Або:

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\Uninstall-Lab4.ps1
```

Це видалить namespace `pythonwiki`, API, SQL Server і PVC у цьому namespace.


## Важливо для v3

Скрипт `deploy\install.bat` автоматично запускає `Install-Lab4.ps1` з пересозданням namespace. Docker image збирається з `--no-cache`, щоб у Kubernetes не потрапляла стара версія API без `/health`. Kubernetes probes перевіряють TCP-порт API, а `/health` використовується для smoke-test і демонстрації load balancing.


## Якщо Smoke test не проходить напряму через LoadBalancer

У Docker Desktop іноді `LoadBalancer` отримує внутрішню адресу Docker-мережі, наприклад `172.18.0.5`, і `http://localhost:8080` може не відкриватися. Це не означає, що деплой зламаний, якщо pod-и мають статус `1/1 Running`.

Перевірити API гарантовано можна через port-forward:

```powershell
kubectl port-forward svc/pythonwiki-api 8080:8080 -n pythonwiki
```

Після цього в іншому терміналі:

```powershell
Invoke-RestMethod http://localhost:8080/health
```

## Update v5

У версії v5 endpoint `/health` реалізовано також як окремий `HealthController`, тому він працює через стандартний `MapControllers()` навіть якщо minimal API endpoint не підхопився. Smoke test у скриптах можна виконувати через `/health`, а якщо треба просто перевірити доступність сервісу — через `/swagger`.


## Примітка щодо PowerShell load test

У dashboard пункт 6 запускає простий PowerShell-навантажувальний тест. За замовчуванням він тестує `/swagger`, тому що цей endpoint гарантовано повертає `200 OK`. Для демонстрації масштабування можна послідовно виставити 1, 2 і 3 replicas через пункт 3, потім для кожного значення запускати пункт 6 і порівнювати `RequestsPerSecond`.


## Fix v8

У версії v8 виправлено PowerShell-навантажувальний тест для Windows PowerShell 5.x. Скрипт більше не використовує `System.Net.Http.HttpClient` у `Start-Job`, тому пункт 6 dashboard не падає з помилкою `Unable to find type [System.Net.Http.HttpClient]`.

Також пункт 5 dashboard тепер, якщо `/health` повертає 404, додатково перевіряє `/swagger`. Це дозволяє не блокувати демонстрацію, якщо у старому зібраному Docker image ще немає health endpoint, але сам API доступний.
