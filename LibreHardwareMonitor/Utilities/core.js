// LHM Dashboard by KyoudaiKen

/*

"#62cbacff",
"#cec46bff",
"#d09771ff",
"#d47e89ff",
"#b891daff",
"#83b0d6ff",
"#8dd993ff",
"#9ecc67ff",
"#de9dcaff",
"#8fdacaff"

*/

//Automatic color palette definitions
const colorPallettes = {
    "default": {
        "lines": [
            "#00ff8dff",
            "#62d0ffff",
            "#007affff",
            "#ff64ffff",
            "#ff006eff",
            "#ffffffff"
        ],
        "fill": [
            "#00ff8d00",
            "#62d0ff00",
            "#007aff00",
            "#ff64ff00",
            "#ff006e00",
            "#ffffff00"
        ],
        "gridlines": [
            "#00ff8d40",
            "#62d0ff40",
            "#007aff40",
            "#ff64ff40",
            "#ff006e40",
            "#ffffff40"
        ]
    }
}

//Define color palette
const cpl = colorPallettes["default"];

//Borders
const widthBorders = 2;
const gridLineWidthX = 1;
const gridLineWidthY = 1;

//Fonts
Chart.defaults.global.defaultFontFamily = "'DejaVu Sans Mono', Monospace";
Chart.defaults.global.defaultFontSize = 22;
Chart.defaults.global.defaultFontColor = "#fff";
const fontSizeLegendY = 16;
const fontSizeLegendX = 16;

//Config
var chartTimeFrame = 60;
var boolPrefillChart = true;

//Globals
var sensorIndex = 0;

document.addEventListener("DOMContentLoaded", function(event) { 
    var TempChart = new Chart(document.getElementById('TempChart').getContext('2d'), {
        type: 'line',
        data: {
            datasets: [
                {
                    label: 'CPU Temperature',
                    data: [],
                    backgroundColor: [cpl['fill'][0]],
                    borderColor: [cpl['lines'][0]],
                    borderWidth: widthBorders
                },
                {
                    label: 'GPU Temperature',
                    data: [],
                    backgroundColor: [cpl['fill'][1]],
                    borderColor: [cpl['lines'][1]],
                    borderWidth: widthBorders
                },
                {
                    label: 'VRM Temperature',
                    data: [],
                    backgroundColor: [cpl['fill'][2]],
                    borderColor: [cpl['lines'][2]],
                    borderWidth: widthBorders
                }
                ,
                {
                    label: 'PCH Temperature',
                    data: [],
                    backgroundColor: [cpl['fill'][3]],
                    borderColor: [cpl['lines'][3]],
                    borderWidth: widthBorders
                }
            ]
        },
        options: {
            maintainAspectRatio: false,
            title: {
                display: true,
                text: 'Mainbaord Temperatures (°C)'
            },
            scales: {
                yAxes: [{
                    ticks: {
                        stepSize: 5,
                        fontSize: fontSizeLegendY
                    },
                    gridLines: {
                        color: cpl['gridlines'][0],
                        lineWidth: gridLineWidthY
                    }
                }],
                xAxes: [{
                    type: 'time',
                    ticks: {
                        display: false
                    },
                    time: {
                        unit: 'second',
                        stepSize: 60,
                        displayFormats: {
                            second: 'HH:mm:ss'
                        }
                    },
                    gridLines: {
                        color: cpl['gridlines'][0],
                        lineWidth: gridLineWidthX
                    }
                }]
            },
            animation: false,
            tooltips: {
                enabled: false
            },
            elements: {
                line: {
                    tension: 0
                },
                point: {
                    radius: 0,
                    borderColor: 'transparent',
                    backgroundColor: 'transparent'
                }
            },
            layout: {
                padding: {
                    left: 0,
                    right: 10,
                    top: 0,
                    bottom: 0
                }
            }
        }
    });

    var TempChart2 = new Chart(document.getElementById('TempChart2').getContext('2d'), {
        type: 'line',
        data: {
            datasets: [
                {
                    label: 'Water Temperature',
                    data: [],
                    backgroundColor: [cpl['fill'][1]],
                    borderColor: [cpl['lines'][1]],
                    borderWidth: widthBorders
                },
                {
                    label: 'Case Temperature',
                    data: [],
                    backgroundColor: [cpl['fill'][2]],
                    borderColor: [cpl['lines'][2]],
                    borderWidth: widthBorders
                },
                {
                    label: 'Ambient Temperature',
                    data: [],
                    backgroundColor: [cpl['fill'][3]],
                    borderColor: [cpl['lines'][3]],
                    borderWidth: widthBorders
                }
            ]
        },
        options: {
            maintainAspectRatio: false,
            title: {
                display: true,
                text: 'Other Temperatures (°C)'
            },
            scales: {
                yAxes: [{
                    ticks: {
                        stepSize: 5,
                        fontSize: fontSizeLegendY
                    },
                    gridLines: {
                        color: cpl['gridlines'][0],
                        lineWidth: gridLineWidthY
                    }
                }],
                xAxes: [{
                    type: 'time',
                    ticks: {
                        display: false
                    },
                    time: {
                        unit: 'second',
                        stepSize: 60,
                        displayFormats: {
                            second: 'HH:mm:ss'
                        }
                    },
                    gridLines: {
                        color: cpl['gridlines'][0],
                        lineWidth: gridLineWidthX
                    }
                }]
            },
            animation: false,
            tooltips: {
                enabled: false
            },
            elements: {
                line: {
                    tension: 0
                },
                point: {
                    radius: 0,
                    borderColor: 'transparent',
                    backgroundColor: 'transparent'
                }
            },
            layout: {
                padding: {
                    left: 0,
                    right: 10,
                    top: 0,
                    bottom: 0
                }
            }
        }
    });

    var ControlChart = new Chart(document.getElementById('ControlChart').getContext('2d'), {
        type: 'line',
        data: {
            datasets: [
                {
                    label: 'Pump',
                    data: [],
                    backgroundColor: [cpl['fill'][0]],
                    borderColor: [cpl['lines'][0]],
                    borderWidth: widthBorders,
                    yAxisID: 'rpm'
                },
                {
                    label: 'Rad',
                    data: [],
                    backgroundColor: [cpl['fill'[1]]],
                    borderColor: [cpl['lines'][1]],
                    borderWidth: widthBorders,
                    yAxisID: 'rpm'
                },
                {
                    label: 'Radiator Fan Speed',
                    data: [],
                    backgroundColor: [cpl['fill'][2]],
                    borderColor: [cpl['lines'][2]],
                    borderWidth: widthBorders,
                    yAxisID: 'pv'
                },
                {
                    label: 'Case Fan Speed',
                    data: [],
                    backgroundColor: [cpl['fill'][3]],
                    borderColor: [cpl['lines'][3]],
                    borderWidth: widthBorders,
                    yAxisID: 'pv'
                }
            ]
        },
        options: {
            maintainAspectRatio: false,
            title: {
                display: true,
                text: 'Control Signals & Monitor'
            },
            scales: {
                yAxes: [
                    {
                        id: 'rpm',
                        ticks: {
                            stepSize: 500,
                            fontSize: fontSizeLegendY
                        },
                        gridLines: {
                            color: cpl['gridlines'][0],
                            lineWidth: gridLineWidthY
                        }
                    },
                    {
                        id: 'pv',
                        ticks: {
                            stepSize: 5,
                            fontSize: fontSizeLegendY
                        },
                        gridLines: {
                            color: cpl['gridlines'][4],
                            lineWidth: gridLineWidthY
                        }
                    }
                ],
                xAxes: [{
                    type: 'time',
                    ticks: {
                        display: false
                    },
                    time: {
                        unit: 'second',
                        stepSize: 60,
                        displayFormats: {
                            second: 'HH:mm:ss'
                        }
                    },
                    gridLines: {
                        color: cpl['gridlines'][0],
                        lineWidth: gridLineWidthX
                    }
                }]
            },
            animation: false,
            tooltips: {
                enabled: false
            },
            elements: {
                line: {
                    tension: 0
                },
                point: {
                    radius: 0,
                    borderColor: 'transparent',
                    backgroundColor: 'transparent'
                }
            },
            layout: {
                padding: {
                    left: 0,
                    right: 10,
                    top: 0,
                    bottom: 0
                }
            }
        }
    });


    var PowerChart = new Chart(document.getElementById('PowerChart').getContext('2d'), {
        type: 'line',
        data: {
            datasets: [
                {
                    label: 'CPU:',
                    data: [],
                    backgroundColor: [cpl['fill'][0]],
                    borderColor: [cpl['lines'][0]],
                    borderWidth: widthBorders
                },
                {
                    label: 'GPU:',
                    data: [],
                    backgroundColor: [cpl['fill'][1]],
                    borderColor: [cpl['lines'][1]],
                    borderWidth: widthBorders
                }
            ]
        },
        options: {
            maintainAspectRatio: false,
            title: {
                display: true,
                text: 'Power Monitor (W)'
            },
            scales: {
                yAxes: [{
                    ticks: {
                        stepSize: 50,
                        fontSize: fontSizeLegendY
                    },
                    gridLines: {
                        color: cpl['gridlines'][0],
                        lineWidth: gridLineWidthY
                    }
                }],
                xAxes: [{
                    type: 'time',
                    ticks: {
                        display: false
                    },
                    time: {
                        unit: 'second',
                        stepSize: 60,
                        displayFormats: {
                            second: 'HH:mm:ss'
                        }
                    },
                    gridLines: {
                        color: cpl['gridlines'][0],
                        lineWidth: gridLineWidthX
                    }
                }]
            },
            animation: false,
            tooltips: {
                enabled: false
            },
            elements: {
                line: {
                    tension: 0
                },
                point: {
                    radius: 0,
                    borderColor: 'transparent',
                    backgroundColor: 'transparent'
                }
            },
            layout: {
                padding: {
                    left: 0,
                    right: 10,
                    top: 0,
                    bottom: 0
                }
            }
        }
    });

    var ClockChart = new Chart(document.getElementById('ClockChart').getContext('2d'), {
        type: 'line',
        data: {
            datasets: [
                {
                    label: 'CPU Max:',
                    data: [],
                    backgroundColor: [cpl['fill'][0]],
                    borderColor: [cpl['lines'][0]],
                    borderWidth: widthBorders
                },
                {
                    label: 'CPU Avg:',
                    data: [],
                    backgroundColor: [cpl['fill'][2]],
                    borderColor: [cpl['lines'][2]],
                    borderWidth: widthBorders
                },
                {
                    label: 'GPU Core:',
                    data: [],
                    backgroundColor: [cpl['fill'][1]],
                    borderColor: [cpl['lines'][1]],
                    borderWidth: widthBorders
                },
                {
                    label: 'GPU Memory:',
                    data: [],
                    backgroundColor: [cpl['fill'][3]],
                    borderColor: [cpl['lines'][3]],
                    borderWidth: widthBorders
                }
            ]
        },
        options: {
            maintainAspectRatio: false,
            title: {
                display: true,
                text: 'Clocks (MHz)'
            },
            scales: {
                yAxes: [{
                    ticks: {
                        stepSize: 500,
                        fontSize: fontSizeLegendY
                    },
                    gridLines: {
                        color: cpl['gridlines'][0],
                        lineWidth: gridLineWidthY
                    }
                }],
                xAxes: [{
                    type: 'time',
                    ticks: {
                        display: false
                    },
                    time: {
                        unit: 'second',
                        stepSize: 60,
                        displayFormats: {
                            second: 'HH:mm:ss'
                        }
                    },
                    gridLines: {
                        color: cpl['gridlines'][0],
                        lineWidth: gridLineWidthX
                    }
                }]
            },
            animation: false,
            tooltips: {
                enabled: false
            },
            elements: {
                line: {
                    tension: 0
                },
                point: {
                    radius: 0,
                    borderColor: 'transparent',
                    backgroundColor: 'transparent'
                }
            },
            layout: {
                padding: {
                    left: 0,
                    right: 10,
                    top: 0,
                    bottom: 0
                }
            }
        }
    });

    var LoadChart = new Chart(document.getElementById('LoadChart').getContext('2d'), {
        type: 'line',
        data: {
            datasets: [
                {
                    label: 'CPU',
                    data: [],
                    backgroundColor: [cpl['fill'][0]],
                    borderColor: [cpl['lines'][0]],
                    borderWidth: widthBorders,
                    yAxisID: 'percent'
                },
                {
                    label: 'GPU C',
                    data: [],
                    backgroundColor: [cpl['fill'][1]],
                    borderColor: [cpl['lines'][1]],
                    borderWidth: widthBorders,
                    yAxisID: 'percent'
                },
                {
                    label: 'GPU MC',
                    data: [],
                    backgroundColor: [cpl['fill'][2]],
                    borderColor: [cpl['lines'][2]],
                    borderWidth: widthBorders,
                    yAxisID: 'percent'
                },
                {
                    label: 'GPU VE',
                    data: [],
                    backgroundColor: [cpl['fill'][3]],
                    borderColor: [cpl['lines'][3]],
                    borderWidth: widthBorders,
                    yAxisID: 'percent'
                },
                {
                    label: 'RAM',
                    data: [],
                    backgroundColor: [cpl['fill'][4]],
                    borderColor: [cpl['lines'][4]],
                    borderWidth: widthBorders,
                    yAxisID: 'GByte'
                },
                {
                    label: 'VRAM',
                    data: [],
                    backgroundColor: [cpl['fill'][5]],
                    borderColor: [cpl['lines'][5]],
                    borderWidth: widthBorders,
                    yAxisID: 'GByte'
                }
            ]
        },
        options: {
            maintainAspectRatio: false,
            title: {
                display: true,
                text: 'Load (%) & RAM (GByte)'
            },
            scales: {
                yAxes: [{
                    id: 'percent',
                    ticks: {
                        stepSize: 10,
                        min: 0,
                        max: 100,
                        fontSize: fontSizeLegendY
                    },
                    gridLines: {
                        color: cpl['gridlines'][0],
                        lineWidth: gridLineWidthY
                    }
                },
                {
                    id: 'GByte',
                    ticks: {
                        stepSize: 4,
                        min: 0,
                        max: 16,
                        fontSize: fontSizeLegendY
                    },
                    gridLines: {
                        color: cpl['gridlines'][1],
                        lineWidth: gridLineWidthY
                    }
                }],
                xAxes: [{
                    type: 'time',
                    ticks: {
                        display: false
                    },
                    time: {
                        unit: 'second',
                        stepSize: 60,
                        displayFormats: {
                            second: 'HH:mm:ss'
                        }
                    },
                    gridLines: {
                        color: cpl['gridlines'][0],
                        lineWidth: gridLineWidthY
                    }
                }]
            },
            animation: false,
            tooltips: {
                enabled: false
            },
            elements: {
                line: {
                    tension: 0
                },
                point: {
                    radius: 0,
                    borderColor: 'transparent',
                    backgroundColor: 'transparent'
                }
            },
            layout: {
                padding: {
                    left: 0,
                    right: 10,
                    top: 0,
                    bottom: 0
                }
            }
        }
    });

    //Helper functions
    function round(value, precision) {
        var multiplier = Math.pow(10, precision || 0);
        return Math.round(value * multiplier) / multiplier;
    }

    function cleanReading(reading) {
        return parseFloat(reading.replace(',', '.').replace(/[^0-9.]/g, ""));
    }

    function prefillChart(chartData) {
        chartData.length = 0;
        var currDateTime = new Date()
        currDateTime.setTime(currDateTime.getTime() - chartTimeFrame * 1000);
        for (var i = 0; i < chartTimeFrame; i++) chartData.push({ x: new Date(currDateTime.getTime() + i * 1000), y: undefined })
    }

    function prefillAllDatasets(chart) {
        for (var i = 0; i < chart.data.datasets.length; i++)
            prefillChart(chart.data.datasets[i].data);
    }

    function cleanupGraph(chartData) {
        if (chartData.length != 0) {
            if (chartData.length >= chartTimeFrame) chartData.splice(0, 1);
            if (new Date().getTime() - chartData[chartData.length - 1].x.getTime() > 5000) {
                if (boolPrefillChart) {
                    prefillChart(chartData);
                } else {
                    chartData.length = 0;
                }
            }
        }
    }

    function plotNextSensor(chart, label, value, special = "", append = "") {
        switch (special) {
            case "cpu cores":
                //Determine max and average core clocks
                var max = 0, total = 0, avg = 0, cores = 0;
                var cores = value.Children[1].Children[1].Children;
                var numCores = cores.length;
                for (c = 0; c < numCores; c++) {
                    var clean = cleanReading(cores[c].Value);
                    if (max < clean) max = clean;
                    total += clean;
                }
                avg = total / numCores;

                //Plot it.
                cleanupGraph(chart.data.datasets[sensorIndex].data);
                chart.data.datasets[sensorIndex].data.push({ x: new Date(), y: max });
                chart.data.datasets[sensorIndex].label = label + " Max: " + round(max, 1).toFixed(1);
                sensorIndex++;

                cleanupGraph(chart.data.datasets[sensorIndex].data);
                chart.data.datasets[sensorIndex].data.push({ x: new Date(), y: avg });
                chart.data.datasets[sensorIndex].label = label + " Avg: " + round(avg, 1).toFixed(1);
                sensorIndex++;
                break;
            default:
                var clean = cleanReading(value);
                if (special == "div2") clean /= 2; //Divides through 2 if requested. Useful to convert DDR Memory GT/s back to MHz.
                if (special == "div4") clean /= 4; //Divides through 4 if requested. Useful to convert QDR Memory GT/s back to MHz.
                if (special == "MB2GB") clean /= 1024;
                cleanupGraph(chart.data.datasets[sensorIndex].data);
                chart.data.datasets[sensorIndex].data.push({ x: new Date(), y: clean });
                chart.data.datasets[sensorIndex].label = label + round(clean, 1).toFixed(1) + append;
                sensorIndex++;
                break;
        }
    }

    //XHR callback
    function refreshSensors() {
        var x = new XMLHttpRequest();
        x.open("GET", "data.json");
        x.addEventListener("load", function (e) {
            var data = JSON.parse(x.response).Children[0];

            //Temperatures and Fans ------------
            //Ryzen TR CPU temp
            plotNextSensor(TempChart, "CPU: ", data.Children[1].Children[2].Children[1].Value);
            //NVIDIA GPU Temp
            plotNextSensor(TempChart, "GPU: ", data.Children[3].Children[1].Children[0].Value);
            //ASUS VRM Temp
            plotNextSensor(TempChart, "VRM: ", data.Children[0].Children[0].Children[1].Children[5].Value);
            //ASUS PCH Temp
            plotNextSensor(TempChart, "PCH: ", data.Children[0].Children[0].Children[1].Children[3].Value);

            //Temperatures 2
            sensorIndex = 0;
            //KyoudaiKen FC01 Water Temp
            plotNextSensor(TempChart2, "Water: ", data.Children[4].Children[0].Children[0].Value);
            //KyoudaiKen FC01 Case Temp
            plotNextSensor(TempChart2, "Case: ", data.Children[4].Children[0].Children[1].Value);
            //KyoudaiKen FC01 Ambient Temp
            plotNextSensor(TempChart2, "Ambient: ", data.Children[4].Children[0].Children[2].Value);

            //Control and monitor
            sensorIndex = 0;
            //ASUS Fan 0
            plotNextSensor(ControlChart, "Pump: ", data.Children[0].Children[0].Children[2].Children[0].Value, "", " RPM");
            //ASUS buggy fan shit (rad)
            for (var f = 1; f < data.Children[0].Children[0].Children[2].Children.length; f++) {
                if (cleanReading(data.Children[0].Children[0].Children[2].Children[f].Value) > 50) plotNextSensor(ControlChart, "Rad: ", data.Children[0].Children[0].Children[2].Children[f].Value, "", " RPM");
            }
            //KyoudaiKen FC01 Water Temp
            plotNextSensor(ControlChart, "Rad Fans: ", data.Children[4].Children[1].Children[0].Value, "", "%");
            //KyoudaiKen FC01 Case Temp
            plotNextSensor(ControlChart, "Case Fans: ", data.Children[4].Children[1].Children[2].Value, "", "%");

            //Power Monitor ------------
            sensorIndex = 0;
            //Ryzen TR CPU package power
            plotNextSensor(PowerChart, "CPU: ", data.Children[1].Children[5].Children[0].Value);
            //NVIDIA GPU power
            plotNextSensor(PowerChart, "GPU: ", data.Children[3].Children[3].Children[0].Value);

            //Clocks ------------
            sensorIndex = 0;
            //CPU Clocks
            plotNextSensor(ClockChart, "CPU", data, "cpu cores");
            //GPU Clocks
            plotNextSensor(ClockChart, "GPU Core: ", data.Children[3].Children[0].Children[0].Value);
            plotNextSensor(ClockChart, "GPU Memory: ", data.Children[3].Children[0].Children[1].Value, "div4");

            //Loads ------------
            sensorIndex = 0;
            //CPU Total
            plotNextSensor(LoadChart, "CPU: ", data.Children[1].Children[3].Children[0].Value);
            //NVIDIA GPU Core
            plotNextSensor(LoadChart, "GPU: ", data.Children[3].Children[2].Children[0].Value);
            //NVIDIA Memory Controller
            plotNextSensor(LoadChart, "GPU MC: ", data.Children[3].Children[2].Children[1].Value);
            //NVIDIA Video Engine
            plotNextSensor(LoadChart, "GPU VE: ", data.Children[3].Children[2].Children[2].Value);
            //Memory
            plotNextSensor(LoadChart, "RAM: ", data.Children[2].Children[1].Children[0].Value);
            //NVIDIA VRAM Load
            plotNextSensor(LoadChart, "GPU MEM: ", data.Children[3].Children[4].Children[1].Value, "MB2GB");

            //Change RAM "load" chart range according to RAM size
            LoadChart.options.scales.yAxes[1].ticks.max = Math.ceil(cleanReading(data.Children[2].Children[1].Children[0].Value) + cleanReading(data.Children[2].Children[1].Children[1].Value));
            LoadChart.options.scales.yAxes[1].ticks.stepSize = LoadChart.options.scales.yAxes[1].ticks.max / 8;

            sensorIndex = 0;
            TempChart.update();
            TempChart2.update();
            ControlChart.update();
            PowerChart.update();
            ClockChart.update();
            LoadChart.update();
        });
        x.send();
        setTimeout(refreshSensors, 1000);
    }

    //Init
    if (boolPrefillChart) {
        prefillAllDatasets(TempChart);
        prefillAllDatasets(TempChart2);
        prefillAllDatasets(ControlChart);
        prefillAllDatasets(PowerChart);
        prefillAllDatasets(ClockChart);
        prefillAllDatasets(LoadChart);
    }
    refreshSensors();
});