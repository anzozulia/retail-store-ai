# RetailStore AI

Курсова робота з дисципліни «Адміністрування та програмування баз даних в штучному інтелекті» (КНЕУ), тема 14 «Розробка бази даних та інтерфейсу користувача для роздрібного бізнесу на основі ШІ». Проєкт складається з бази даних MS SQL Server для обліку товарів і продажів одного магазину, веб-інтерфейсу на ASP.NET Core MVC (англійською мовою) і модуля прогнозування попиту. Прогноз тижневих продажів кожного товару, перевірку прогнозу на 12 попередніх тижнях і рекомендований обсяг замовлення розраховує процедура, що зберігається, `dbo.usp_CalculateDemandForecast`.

- Застосунок: https://retailstore.anzozulia.com
- Репозиторій: https://github.com/anzozulia/retail-store-ai

## Стек

- MS SQL Server 2022, T-SQL (таблиці з обмеженнями, функція, представлення, тригер, процедури, що зберігаються)
- .NET 10, ASP.NET Core MVC, Entity Framework Core 10 (Database First)
- Bootstrap 5 (тема Bootswatch Flatly), Bootstrap Icons, Chart.js 4
- Docker, Docker Compose, Caddy (HTTPS із сертифікатом Let’s Encrypt), GitHub Actions

## Структура репозиторію

| Шлях | Призначення |
|---|---|
| `db/` | скрипти T-SQL `00`…`06` і `init-db.sh` для створення, заповнення та перезаповнення бази |
| `src/RetailStore.Web/` | застосунок ASP.NET Core MVC |
| `Dockerfile` | збирання образу застосунку |
| `docker-compose.yml` | SQL Server, ініціалізація бази, застосунок і Caddy на сервері |
| `Caddyfile` | HTTPS і зворотний проксі на застосунок |
| `.github/workflows/deploy.yml` | збирання та розгортання через GitHub Actions |
| `run_local.command` | локальний запуск на macOS |
| `.env.example` | приклад файлу `.env` з паролями |

Скрипти бази даних:

| Скрипт | Дія |
|---|---|
| `00_drop_database.sql` | видалення бази (тільки для перезаповнення) |
| `01_create_database.sql` | створення бази `RetailStoreDb` з файлами даних і журналу |
| `02_create_tables.sql` | таблиці, обмеження, індекси |
| `03_create_objects.sql` | функція, представлення, тригер, процедури |
| `04_seed_data.sql` | тестові дані за 26 тижнів і перший розрахунок прогнозу |
| `05_security.sql` | користувач застосунку `retail_app` з мінімальними правами |
| `06_tests.sql` | перевірка обмежень, тригера і прогнозу |

## Локальний запуск (macOS)

Потрібні Docker Desktop і .NET 10 SDK. На Mac з процесором Apple Silicon у налаштуваннях Docker Desktop має бути ввімкнено Rosetta для емуляції x86_64/amd64, бо образ SQL Server існує тільки для amd64.

Запуск: двічі клацнути `run_local.command` у Finder або виконати в терміналі з кореня репозиторію

```bash
./run_local.command
```

Скрипт виконує такі кроки:

1. Створює файл `.env` з випадковими паролями, якщо його ще немає.
2. Запускає контейнер `retailstore-mssql` з SQL Server 2022 на порту `14330`.
3. Створює і заповнює базу `RetailStoreDb`, якщо її ще немає, та оновлює пароль користувача `retail_app`.
4. Запускає застосунок на http://localhost:5080 і відкриває його в браузері.

Тестові дані генеруються відносно дати заповнення бази. Щоб перестворити базу зі свіжими даними:

```bash
./run_local.command --reset
```

## Оновлення моделей EF Core (Database First)

Класи сутностей у `Models/` і контекст `Data/RetailStoreContext.cs` згенеровано з наявної бази. Після зміни схеми їх генерують повторно. Рядок підключення береться зі змінної середовища, тому пароль не потрапляє в код.

```bash
dotnet tool install --global dotnet-ef --version 10.0.15
set -a && source .env && set +a
export RETAILSTORE_DB="Server=localhost,14330;Database=RetailStoreDb;User Id=sa;Password=$SA_PASSWORD;TrustServerCertificate=True"
cd src/RetailStore.Web
dotnet ef dbcontext scaffold "$RETAILSTORE_DB" Microsoft.EntityFrameworkCore.SqlServer \
  --output-dir Models --context-dir Data --context RetailStoreContext \
  --namespace RetailStore.Web.Models --context-namespace RetailStore.Web.Data \
  --no-onconfiguring --force
```

Параметр `--no-onconfiguring` не додає рядок підключення в контекст. Застосунок отримує його з конфігурації `ConnectionStrings:RetailStore` (локально її задає `run_local.command`, на сервері `docker-compose.yml`).

## Розгортання

Кожен push у гілку `main` запускає workflow «Build and deploy» (його також можна запустити вручну на вкладці Actions):

1. Job `build` збирає проєкт командою `dotnet build -c Release`.
2. Job `deploy` підключається до сервера по SSH, записує `/opt/retailstore/.env` з GitHub Secrets, оновлює код до `origin/main` і виконує `docker compose up -d --build --remove-orphans`.

На сервері працюють чотири сервіси Docker Compose:

- `db` з SQL Server 2022 (порт назовні не відкривається, дані зберігаються в томі `mssql-data`)
- `db-init`, який чекає на SQL Server, створює і заповнює базу, якщо її ще немає, оновлює функцію, представлення, тригер і процедури, оновлює пароль `retail_app` і завершує роботу
- `app` із застосунком ASP.NET Core на порту 8080 у внутрішній мережі Docker
- `caddy`, який отримує сертифікат Let’s Encrypt і передає запити з портів 80 і 443 до `app`

### GitHub Secrets

| Секрет | Значення |
|---|---|
| `SSH_PRIVATE_KEY` | приватний ключ ed25519 без парольної фрази для користувача `deploy` |
| `SSH_KNOWN_HOSTS` | результат `ssh-keyscan -t ed25519 <SSH_HOST>` |
| `SSH_HOST` | домен або IP-адреса сервера |
| `SSH_USER` | `deploy` |
| `SA_PASSWORD` | пароль адміністратора SQL Server `sa` |
| `APP_PASSWORD` | пароль користувача бази `retail_app` |

Паролі мають містити щонайменше 8 символів, великі й малі літери та цифри (вимога політики паролів SQL Server). Крапка з комою, знак долара, лапки й пробіли в паролях не використовуються, бо пароль входить у рядок підключення та файл `.env`. Пароль `sa` застосовується тільки під час першого запуску SQL Server з порожнім томом, тому після цього його не змінюють. Пароль `retail_app` можна змінити в будь-який момент, бо `db-init` оновлює його під час кожного розгортання.

### Одноразове налаштування сервера

Сервер: Ubuntu 26.04 x86_64, DNS-запис `retailstore.anzozulia.com` вказує на його IP-адресу.

1. Встановити Docker з репозиторію Ubuntu:
   ```bash
   sudo apt-get install -y docker.io docker-compose-v2 docker-buildx
   ```
2. Створити користувача `deploy` і додати його до групи `docker`:
   ```bash
   sudo adduser --disabled-password --gecos "" deploy
   sudo usermod -aG docker deploy
   ```
3. Створити пару ключів на своєму комп’ютері (`ssh-keygen -t ed25519 -N "" -f retailstore_deploy`), додати публічний ключ у `/home/deploy/.ssh/authorized_keys`, а приватний зберегти в секреті `SSH_PRIVATE_KEY`.
4. Клонувати репозиторій у `/opt/retailstore`:
   ```bash
   sudo mkdir /opt/retailstore
   sudo chown deploy:deploy /opt/retailstore
   sudo -u deploy git clone https://github.com/anzozulia/retail-store-ai.git /opt/retailstore
   ```
5. Дозволити в брандмауері порти 22/tcp, 80/tcp, 443/tcp і 443/udp.
6. Оскільки SQL Server використовує до 2 ГБ пам’яті, а образ застосунку збирається на сервері, варто додати файл підкачки на 2 ГБ:
   ```bash
   sudo fallocate -l 2G /swapfile
   sudo chmod 600 /swapfile
   sudo mkswap /swapfile
   sudo swapon /swapfile
   echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
   ```
7. Додати всі секрети в репозиторії (Settings, Secrets and variables, Actions) і запустити workflow. Перше розгортання триває кілька хвилин: завантаження образу SQL Server, збирання застосунку, заповнення бази й отримання сертифіката.

Перевірка стану на сервері:

```bash
cd /opt/retailstore
docker compose ps
docker compose logs -f app
```

### Перезаповнення демонстраційних даних на сервері

```bash
cd /opt/retailstore && docker compose run --rm db-init --reset
```

Команда видаляє базу `RetailStoreDb`, створює її заново з тестовими даними відносно поточної дати, розраховує прогноз і відновлює права `retail_app`. Застосунок перезапускати не потрібно.
