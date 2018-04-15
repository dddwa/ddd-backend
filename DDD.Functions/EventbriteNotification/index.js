var request = require('request');

function getDeDupeRowKey(payload) {
    return payload.action + "|" + payload.id.replace(/[\/#\\\?]/g, "");
}

function deDupeWebhookRequest(payload, log, successAction, duplicateAction, errorAction) {
    var azure = require('azure-storage');
    var tableService = azure.createTableService();
    tableService.createTableIfNotExists('webhookdedupe', function(error, result, response) {
        if (!error) {
            tableService.retrieveEntity('webhookdedupe', payload.webhook, getDeDupeRowKey(payload), function(error, result, response) {
                if (!error) {
                    log("Found duplicate web hook request", payload);
                    duplicateAction();
                } else {
                    successAction();
                }
            });
        } else {
            log("Error deduplication webhook request", error, response);
            errorAction();
        }
    });

    
}

function preventWebhookRequestDuplication(payload, log, successAction, errorAction) {
    var azure = require('azure-storage');
    var tableService = azure.createTableService();
    
    var gen = azure.TableUtilities.entityGenerator;
    var entity = {
        PartitionKey: gen.String(payload.webhook),
        RowKey: gen.String(getDeDupeRowKey(payload))
    };
    tableService.insertEntity('webhookdedupe', entity, function(error, result, response) {
        if (!error) {
            successAction();
        } else {
            log("Error adding deduplication token", error, response);
            errorAction();
        }
    });
}

module.exports = function (context, req) {
    context.log(req.body);

    function end(statusCode) {
        context.res = {
            status: statusCode,
            body: ""
        };
        context.done();
    }

    var deDupePayload = {
        webhook: "eventbrite",
        action: req.body.config.action,
        id: req.body.api_url
    };

    deDupeWebhookRequest(deDupePayload, context.log, function() {
        if (req.body.config.action !== 'order.placed') {
            end(200);
            return;
        }

        var orderUrl = req.body.api_url + "?expand=attendees,event,attendees.ticket_class";
        var bearer = process.env["EventbriteApiBearerToken"]
        request.get(orderUrl, {auth: {bearer: bearer}}, function(error, response, body) {

            try {

                var bodyAsJson = JSON.parse(body);

                if (response && response.statusCode && response.statusCode === 200) {
                    var attendees = bodyAsJson.attendees.map(function(attendee) {
                        return {
                            name: attendee.profile.name,
                            event: bodyAsJson.event.name.text,
                            ticketClass: attendee.ticket_class_name,
                            qtySold: attendee.ticket_class.quantity_sold,
                            totalQty: attendee.ticket_class.quantity_total,
                            orderId: attendee.order_id
                        };
                    });
                    context.log("Attendee(s):", attendees);

                    context.bindings.queue = attendees.map(function(x){return JSON.stringify(x);});

                    preventWebhookRequestDuplication(deDupePayload, context.log, function() {
                        end(200);
                    }, function() {
                        end(500);
                    });

                } else {
                    context.log("ERROR calling EventBrite: (", response.statusCode, ") ", body);
                    end(500);
                }
            } catch (e) {
                context.log("ERROR thrown", e);
                end(500);
            }
        });
    }, function() {
        end(200);
    }, function() {
        end(500);
    });
};

/*
Test using:
{
    config: 
    {
        action: 'order.placed',
        user_id: '141671750594',
        endpoint_url: 'https://dddperth-eventbritewebhook.azurewebsites.net/api/HttpTriggerJS1?code=...',
        webhook_id: '438789'
    },
    api_url: 'https://www.eventbriteapi.com/v3/orders/650520140/'
}
*/