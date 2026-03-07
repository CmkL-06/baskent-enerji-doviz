import requests, os
from dotenv import load_dotenv

load_dotenv()

# Localhost: BASKENT_API_URL=http://localhost:5093/api/v1 veya .env
base_url = os.getenv("BASKENT_API_URL", "https://api.baskentenerji.com/api/v1")
url = base_url.rstrip("/") + "/User/login"

data = {
    "mail": os.getenv("BASKENT_USERNAME"),
    "password": os.getenv("BASKENT_PASSWORD")
}

r = requests.post(url, json=data)

print("Status:", r.status_code)
print("Response:", r.text)

if r.status_code == 200:
    token = r.json().get("apiToken")
    if token:
        print("Token:", token[:20] + "...")
