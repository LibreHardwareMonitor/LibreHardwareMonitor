################################################################
# Install:
# * python -m pip install requests
#
# Usage:
# * python basicrest.py
#
# Description:
# A simple test of the REST Like API
################################################################

import json, requests
import time

url = 'http://127.0.0.1:8085'

def findSensors(node):
	sensors = {}

	if len(node["Children"]) > 0:
		for child in node["Children"]:
			sensors.update(findSensors(child))
	else:
		if "Type" in node:
			sensors[node["SensorId"]] = node

	return sensors

def getValue(sensorId):
	params=dict(id=sensorId, action="Get")
	resp = requests.post(url=url + "/Sensor", params = params, timeout=10);
	result = json.loads(resp.text);

	if result["result"] != "ok":
		raise Exception("Server returned error:\n " + result["message"].replace("\\n", "\n").replace("\\r", ""))
	if result["value"] == None:
		return None;
	else:
		return float(result["value"])

def setValue(sensorId, sensorValue):
	if sensorValue == None:
		sensorValue = "null"
	params=dict(id=sensorId, action="Set", value=sensorValue)
	resp = requests.post(url=url + "/Sensor", params = params, timeout=10);
	result = json.loads(resp.text)
	if result["result"] != "ok":
		raise Exception("Server returned error:\n " + result["message"].replace("\\n", "\n").replace("\\r", ""))

def main():
	params = dict()

	print("Fetching all sensor ids:")
	resp = requests.get(url=url + "/data.json", params=params, timeout=10)
	data = json.loads(resp.text)
	sensors = findSensors(data)

	for key, value in sensors.items():
		v = getValue(key)
		print(key, ":", v);

	# Change the id to one of yours 
	print("Setting GPU Fan to full speed!")
	setValue("/nvidiagpu/0/control/0", "100.0")
	time.sleep(10);
	print("Returning GPU Fan speed to default")
	setValue("/nvidiagpu/0/control/0", None)


if __name__ == '__main__':
    main()