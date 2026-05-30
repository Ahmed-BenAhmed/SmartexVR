# Backend Setup Guide

## Quick Start

### Prerequisites
- Python 3.10+
- Git
- Virtual environment support

### 1. Navigate to Backend
```bash
cd backend/services/maintenance
```

### 2. Create Python Environment
```bash
python -m venv venv
```

### 3. Activate Environment

**Windows (PowerShell):**
```powershell
.\venv\Scripts\Activate.ps1
```

**Windows (CMD):**
```cmd
venv\Scripts\activate.bat
```

**macOS/Linux:**
```bash
source venv/bin/activate
```

### 4. Install Dependencies
```bash
pip install -r requirements.txt
```

### 5. Run Server
```bash
python -m uvicorn app.main:app --reload --host 0.0.0.0 --port 8000
```

**Expected output:**
```
INFO:     Uvicorn running on http://0.0.0.0:8000 (Press CTRL+C to quit)
INFO:     Started reloader process [12345] using StatReload
```

---

## Access API

### Swagger UI (Interactive Docs)
```
http://localhost:8000/docs
```
Test endpoints directly in the browser

### ReDoc (API Reference)
```
http://localhost:8000/redoc
```
Beautiful API documentation

### Health Check
```bash
curl http://localhost:8000/health
# Response: {"status": "healthy"}
```

---

## Project Structure

```
backend/services/maintenance/
├── app/
│   ├── __init__.py
│   ├── main.py                 # FastAPI app entry point
│   ├── models/
│   │   ├── __init__.py
│   │   └── maintenance.py      # Pydantic request/response models
│   ├── routers/
│   │   ├── __init__.py
│   │   └── maintenance.py      # Endpoint handlers
│   └── db/
│       ├── __init__.py
│       ├── database.py         # SQLAlchemy session & engine
│       └── models.py           # ORM models (MaintenanceLogDB)
├── tests/
│   ├── __init__.py
│   └── test_api.py            # 13 pytest tests
├── requirements.txt            # Python dependencies
├── maintenance.db              # SQLite database (auto-created)
└── README.md
```

---

## Running Tests

### Run All Tests
```bash
python -m pytest tests/test_api.py -v
```

### Run Specific Test
```bash
python -m pytest tests/test_api.py::TestHealthEndpoints -v
```

### Run with Coverage
```bash
python -m pytest tests/test_api.py --cov=app
```

**Expected Result:**
```
13 passed in 1.85s ✓
```

---

## Environment Variables

### Development (Default)
```bash
# No config needed - uses defaults
export DATABASE_URL=sqlite:///./maintenance.db
export PORT=8000
export ENV=development
```

### Production (PostgreSQL)
```bash
export DATABASE_URL=postgresql://user:password@localhost/smartex_maintenance
export PORT=8000
export ENV=production
```

---

## Database

### SQLite (Development)
- Auto-created at `maintenance.db`
- No setup required
- Perfect for local development

### PostgreSQL (Production)
To migrate to PostgreSQL:

1. **Install PostgreSQL** driver:
   ```bash
   pip install psycopg2-binary
   ```

2. **Set connection string:**
   ```bash
   export DATABASE_URL=postgresql://user:pass@localhost:5432/smartex_maintenance
   ```

3. **Restart server** - Tables auto-created on startup

---

## API Schema

### Database Schema (Auto-Created)

**maintenance_logs table**
```sql
CREATE TABLE maintenance_logs (
    id INTEGER PRIMARY KEY,
    device_id VARCHAR,
    procedure_id VARCHAR,
    completed_steps JSON,
    user_id VARCHAR,
    notes VARCHAR,
    created_at DATETIME,
    updated_at DATETIME
);
```

**procedures table** (optional, for future use)
```sql
CREATE TABLE procedures (
    id INTEGER PRIMARY KEY,
    procedure_id VARCHAR UNIQUE,
    device_id VARCHAR,
    title VARCHAR,
    schema_version INTEGER,
    steps JSON,
    is_active BOOLEAN,
    created_at DATETIME
);
```

---

## Troubleshooting

### Port Already in Use
```bash
# Linux/macOS
lsof -i :8000
kill -9 <PID>

# Windows
netstat -ano | findstr :8000
taskkill /PID <PID> /F
```

### ModuleNotFoundError
```bash
# Make sure virtual environment is activated
# On Windows PowerShell:
.\venv\Scripts\Activate.ps1

# Then reinstall
pip install -r requirements.txt
```

### Database Locked
```bash
# Remove old database and restart
rm maintenance.db
python -m uvicorn app.main:app --reload
```

### CORS Issues in Unity
Already configured! Endpoints accept requests from anywhere:
```python
# In app/main.py
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # Allow all (restrict in production)
)
```

---

## Development Workflow

### Making Changes
```bash
# Activate venv
.\venv\Scripts\Activate.ps1

# Start server (--reload watches for changes)
python -m uvicorn app.main:app --reload --host 0.0.0.0 --port 8000

# In another terminal, run tests
python -m pytest tests/test_api.py -v
```

### Adding New Endpoint
1. Create model in `app/models/maintenance.py`
2. Create handler in `app/routers/maintenance.py`
3. Add route decorator (`@router.get()`, `@router.post()`)
4. Write test in `tests/test_api.py`
5. Test with `pytest`

### Adding New Dependency
```bash
pip install <package>
pip freeze > requirements.txt
```

---

## Deployment

### Docker (Recommended)
```dockerfile
FROM python:3.10-slim
WORKDIR /app
COPY requirements.txt .
RUN pip install -r requirements.txt
COPY app/ app/
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
```

### Heroku
```bash
heroku create smartex-maintenance
git push heroku main
heroku config:set DATABASE_URL=postgresql://...
```

### AWS/GCP
Use serverless options (Lambda, Cloud Run) with PostgreSQL backend

---

## Performance Notes

- **SQLite:** ~1000 concurrent connections (fine for dev/small teams)
- **PostgreSQL:** Enterprise-grade, recommended for production
- **Response time:** <50ms for typical requests
- **Database:** Indexes on `device_id` and `procedure_id` for fast lookups

---

## Support & Issues

See backend repo: `https://github.com/Ahmed-BenAhmed/smartex/tree/master/services/maintenance`

For bugs or questions, create an issue in the repo.
