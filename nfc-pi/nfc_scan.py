import board
import busio
import time
import requests
from adafruit_pn532.i2c import PN532_I2C

# ChronoAPI erreichbar unter (HTTP, siehe ChronoAPI/appsettings.json):
API_URL = "http://192.168.137.11:5088/api/Nfc/scan"

i2c = busio.I2C(board.SCL, board.SDA)
pn532 = PN532_I2C(i2c, debug=False)
pn532.SAM_configuration()

print("Karten nacheinander auflegen (Strg+C zum Beenden)\n")

gesehen = set()

while True:
    uid = pn532.read_passive_target(timeout=0.5)
    if uid is not None:
        uid_hex = "".join([f"{b:02X}" for b in uid])
        if uid_hex not in gesehen:
            gesehen.add(uid_hex)
            print(f"NEUE Karte #{len(gesehen)}: {uid_hex}")

        # An ChronoAPI melden, damit sie im Mitarbeiter-Formular
        # per "Karte scannen"-Button automatisch uebernommen werden kann.
        # Best effort: wenn die API nicht erreichbar ist, soll das
        # Scannen selbst (Konsolen-Ausgabe) trotzdem weiterlaufen.
        try:
            requests.post(API_URL, json={"uid": uid_hex}, timeout=2)
        except requests.RequestException:
            pass

        time.sleep(1)
