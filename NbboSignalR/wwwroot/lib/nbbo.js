if (!String.prototype.supplant) {
    String.prototype.supplant = function (o) {
        return this.replace(/{([^{}]*)}/g,
            function (a, b) {
                var r = o[b];
                return typeof r === 'string' || typeof r === 'number' ? r : a;
            }
        );
    };
}


var nbboTable = document.getElementById('nbboTable');
var nbboTableBody = nbboTable.getElementsByTagName('tbody')[0];
var nbboRowTemplate = '<td>{symbol}</td><td>{bid}</td><td>{bidSize}</td><td>{ask}</td><td>{askSize}</td>';

var depthTable = document.getElementById('depthTable');
var depthTableBody = depthTable.getElementsByTagName('tbody')[0];
var depthRowTemplate = '<td>{symbol}</td><td>{exchange}</td><td>{bid}</td><td>{bidSize}</td><td>{ask}</td><td>{askSize}</td><td>{last}</td>';


let nbboConnection = new signalR.HubConnectionBuilder()
    .withUrl("/nbbo")
    .build();

function createNbboNode(nbbo, type, template) {
    var child = document.createElement(type);
    child.setAttribute('data-symbol', nbbo.symbol);
    child.setAttribute('class', nbbo.symbol);
    child.innerHTML = template.supplant(nbbo);
    return child;
}

function addOrReplaceNbbo(table, nbbo, type, template) {
    var child = createNbboNode(nbbo, type, template);

    // try to replace
    var nbboNode = document.querySelector(type + "[data-symbol=" + nbbo.symbol + "]");
    if (nbboNode) {
        table.replaceChild(child, nbboNode);
    } else {
        // add new stock
        table.appendChild(child);
    }
}

function displayNbbo(nbbo) {
    addOrReplaceNbbo(nbboTableBody, nbbo, 'tr', nbboRowTemplate);
}

nbboConnection.start().then(function () {
    startNbboStreaming();
});

function startNbboStreaming() {
    nbboConnection.stream("StreamNBBOs").subscribe({
        close: false,
        next: displayNbbo,
        error: function (err) {
            console.log(err);
        }
    });
}

var depthSubject = null;
var subscribedSymbol;

function SubscribeToDepth(symbol) {
    subscribedSymbol = symbol.toUpperCase();

    if (depthSubject !== null)
        depthSubject.observers[0].close = true;

    depthSubject = nbboConnection.stream("StreamDepth", subscribedSymbol);

    removeAllChildNodes(depthTableBody);

    depthSubject.subscribe({
        close: false,
        next: displayDepth,
        error: function (err) {
            console.log(err);
        }
    });
}


function createDepthNode(depth, type, template) {
    var child = document.createElement(type);
    child.setAttribute('data-exchange', depth.exchange);
    child.setAttribute('class', depth.symbol);
    child.innerHTML = template.supplant(depth);
    return child;
}

function addOrReplaceDepth(table, depth, type, template) {
    var child = createDepthNode(depth, type, template);

    // try to replace
    var depthNode = document.querySelector(type + "[data-exchange=" + depth.exchange + "]");
    if (depthNode) {
        table.replaceChild(child, depthNode);
    } else {
        // add new stock
        table.appendChild(child);
    }
}

function removeAllChildNodes(parent) {
    while (parent.firstChild) {
        parent.removeChild(parent.firstChild);
    }
}

function displayDepth(depth) {
    if (depth.symbol !== subscribedSymbol) return;
    
   addOrReplaceDepth(depthTableBody, depth, 'tr', depthRowTemplate);
    
}