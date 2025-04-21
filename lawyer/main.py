import requests


url = "https://localhost:7130/api/FileDownload/Download"
output_file_path = "downloaded_from_api.docx"


payload = {
    "fileId": "a22bdd9d-480b-4575-a333-b932c827c30b",
    "email": "joeborg@gmail.com",
    "accessCode": "dcec0240-7d62-4122-b9dc-406ff6ff8463"
}

try:
    response = requests.post(url, json=payload, verify=False)
    response.raise_for_status()

    with open(output_file_path, "wb") as f:
        f.write(response.content)

    print(f"✅ File downloaded successfully as: {output_file_path}")

except requests.exceptions.RequestException as e:
    print("❌ Error downloading file:", e)
