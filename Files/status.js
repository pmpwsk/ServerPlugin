let statusElem = document.querySelector("#status");
let statusEvent = new EventSource('/event[PATH_PREFIX]/status');
onbeforeunload = (event) => { statusEvent.close(); };
statusEvent.onmessage = function (event) {
    if (!event.data.startsWith(":")) {
        statusElem.innerHTML = event.data;
        if (event.data.startsWith("<h"))
            statusElem.className = "elem red";
        else statusElem.className = "elem";
    }
};
