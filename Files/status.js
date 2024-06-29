let statusElem = document.querySelector("#status");
let statusEvent = new EventSource('[PATH_PREFIX]/status-event');
onbeforeunload = (event) => { statusEvent.close(); };
statusEvent.onmessage = function (event) {
    if (!event.data.startsWith(":")) {
        statusElem.innerHTML = event.data;
        if (event.data.startsWith("<h"))
            statusElem.className = "elem red";
        else statusElem.className = "elem";
    }
};
