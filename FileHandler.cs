using System.Text;

namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin : Plugin
{
    public override byte[]? GetFile(string relPath, string pathPrefix, string domain)
    {
        string pluginHome = pathPrefix == "" ? "/" : pathPrefix;
        return relPath switch
        {
            "/log.js" => Encoding.UTF8.GetBytes($"async function Clear() {{\r\n    let response = await fetch('/api{pathPrefix}/clear-log');\r\n    await response.text();\r\n    window.location.reload();\r\n}}"),
            "/send-mail.js" => Encoding.UTF8.GetBytes($"let to = document.querySelector(\"#to\");\r\nlet from = document.querySelector(\"#from\");\r\nlet subject = document.querySelector(\"#subject\");\r\nlet text = document.querySelector(\"#text\");\r\n\r\nasync function Send() {{\r\n    if (to.value === \"\" || from.value === \"\" || subject.value === \"\" || text.value === \"\") {{\r\n        ShowError(\"Fill out everything.\");\r\n    }} else {{\r\n        let response = await fetch(\"/api{pathPrefix}/send-mail?to=\" + encodeURIComponent(to.value) + \"&from=\" + encodeURIComponent(from.value) + \"&subject=\" + encodeURIComponent(subject.value) + \"&text=\" + encodeURIComponent(text.value));\r\n        if (response.status === 200) {{\r\n            let text = await response.text();\r\n            ShowError(text);\r\n        }} else {{\r\n            ShowError(\"Connection failed.\");\r\n        }}\r\n    }}\r\n}}"),
            "/ssh-menu.js" => Encoding.UTF8.GetBytes($"async function Disable(user) {{\r\n    let response = await fetch('/api{pathPrefix}/ssh/disable?user=' + user);\r\n    await response.text();\r\n    window.location.reload();\r\n}}\r\n\r\nasync function Enable(user) {{\r\n    let response = await fetch('/api{pathPrefix}/ssh/enable?user=' + user);\r\n    await response.text();\r\n    window.location.reload();\r\n}}\r\n\r\nasync function Block() {{\r\n    let response = await fetch('/api{pathPrefix}/ssh/block');\r\n    await response.text();\r\n    window.location.reload();\r\n}}\r\n\r\nasync function Change() {{\r\n    let response = await fetch('/api{pathPrefix}/ssh/change');\r\n    await response.text();\r\n    window.location.reload();\r\n}}\r\n\r\nasync function Allow() {{\r\n    let response = await fetch('/api{pathPrefix}/ssh/allow');\r\n    await response.text();\r\n    window.location.reload();\r\n}}"),
            "/ssh-user.js" => Encoding.UTF8.GetBytes($"let to = document.querySelector(\"#to\");\r\nlet from = document.querySelector(\"#from\");\r\nlet subject = document.querySelector(\"#subject\");\r\nlet text = document.querySelector(\"#text\");\r\n\r\nfunction GetUser() {{\r\n    let query = new URLSearchParams(window.location.search);\r\n    return query.get(\"user\");\r\n}}\r\n\r\nasync function Enable() {{\r\n    let response = await fetch(\"/api{pathPrefix}/ssh/enable?user=\" + GetUser());\r\n    if (response.status === 200) {{\r\n        let text = await response.text();\r\n        if (text === \"ok\") {{\r\n            window.location.reload();\r\n        }} else {{\r\n            ShowError(\"Connection failed.\");\r\n        }}\r\n    }} else {{\r\n        ShowError(\"Connection failed.\");\r\n    }}\r\n}}\r\n\r\nasync function Disable() {{\r\n    let response = await fetch(\"/api{pathPrefix}/ssh/disable?user=\" + GetUser());\r\n    if (response.status === 200) {{\r\n        let text = await response.text();\r\n        if (text === \"ok\") {{\r\n            window.location.reload();\r\n        }} else {{\r\n            ShowError(\"Connection failed.\");\r\n        }}\r\n    }} else {{\r\n        ShowError(\"Connection failed.\");\r\n    }}\r\n}}\r\n\r\nasync function Delete(tbd) {{\r\n    let response = await fetch(\"/api{pathPrefix}/ssh/delete?user=\" + GetUser() + \"&pk=\" + tbd);\r\n    if (response.status === 200) {{\r\n        let text = await response.text();\r\n        if (text === \"ok\") {{\r\n            window.location.reload();\r\n        }} else {{\r\n            ShowError(\"Connection failed.\");\r\n        }}\r\n    }} else {{\r\n        ShowError(\"Connection failed.\");\r\n    }}\r\n}}\r\n\r\nasync function Add() {{\r\n    let pk = document.querySelector(\"#pk\");\r\n    if (pk.value === \"\") {{\r\n        ShowError(\"Enter a public key.\");\r\n    }} else {{\r\n        let response = await fetch(\"/api{pathPrefix}/ssh/add?user=\" + GetUser() + \"&pk=\" + encodeURIComponent(pk.value));\r\n        if (response.status === 200) {{\r\n            let text = await response.text();\r\n            if (text === \"ok\") {{\r\n                window.location.reload();\r\n            }} else {{\r\n                ShowError(\"Connection failed.\");\r\n            }}\r\n        }} else {{\r\n            ShowError(\"Connection failed.\");\r\n        }}\r\n    }}\r\n}}"),
            "/update.js" => Encoding.UTF8.GetBytes($"async function Update() {{\r\n    let file = document.getElementById('update-file').files[0];\r\n    let form = new FormData();\r\n    form.append('file', file);\r\n\r\n    let request = new XMLHttpRequest();\r\n    request.open('POST', '{pathPrefix}/update');\r\n    request.upload.addEventListener('progress', event => {{\r\n        document.querySelector('#updateButton').firstElementChild.innerText = 'Updating... ' + ((event.loaded / event.total) * 100).toFixed(2) + '%';\r\n        console.log();\r\n    }});\r\n    request.onreadystatechange = () => {{\r\n        if (request.readyState == 4) {{\r\n            if (request.status === 200) {{\r\n                window.location.reload();\r\n            }} else {{\r\n                alert(\"Error!\");\r\n            }}\r\n        }}\r\n    }};\r\n    request.send(form);\r\n}}"),
            _ => null
        };
    }
    
    public override string? GetFileVersion(string relPath)
    {
        return relPath switch
        {
            "/log.js" => "638295559877667170",
            "/send-mail.js" => "638295523542946760",
            "/ssh-menu.js" => "638295561703447821",
            "/ssh-user.js" => "638295524149147527",
            "/update.js" => "638295526292360126",
            _ => null
        };
    }
}
