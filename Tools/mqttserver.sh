#!/usr/bin/env bash
if [ ! -f /usr/local/bin/pitempmqtt/appsettings.Production.json ]; then
    SERVER="$(whiptail --inputbox "Enter youre MQTT Server address!" --title "MQTT Server" 8 78 "" 3>&1 1>&2 2>&3)"

    if [ -n "$SERVER" ]; then
        sudo sh -c "echo '{ \"MQTT\": {\"Server\": \"$SERVER\" } }' >> /usr/local/bin/pitempmqtt/appsettings.Production.json"
    fi
fi
