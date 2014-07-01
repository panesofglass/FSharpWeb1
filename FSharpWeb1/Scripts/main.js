$(function () {
    var uri = //'api/cars';
        'salespeople?topN=3&region=United%20States&sales=100000';

    $.getJSON(uri)
        .done(function (data) {
            $.each(data, function (key, item) {
                $('<tr><td>' + item.businessEntityID + '</td><td>' +
                    item.firstName + ' ' + item.lastName + '</td><td>' +
                    item.salesYTD + '</td></tr>')
                    .appendTo($('#cars tbody'));
            });
        });
});
