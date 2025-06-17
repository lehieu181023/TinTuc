var BaseController = {
    loaddata: function (controllerName, targetDiv = "#listdata") {
        var dataRequest = {};

        $("#search").find("input, select, textarea").each(function () {
            var $el = $(this);
            var name = $el.attr("name") || $el.attr("id");
            if (!name || $el.prop("disabled")) return;

            var rawValue = $el.val();

            if (Array.isArray(rawValue)) {
                // select[multiple] => nối thành chuỗi "1,2,3"
                if (rawValue.length > 0) {
                    dataRequest[name] = rawValue.join(",");
                }
            } else {
                var value = (rawValue ?? "").toString().trim();
                if (value !== "") {
                    dataRequest[name] = value;
                }
            }
        });

        BlockUI();

        $.ajax({
            url: `/${controllerName}/Listdata`,
            type: "GET",
            data: dataRequest,
            success: function (response) {
                if (response.success = false) {
                    showToast(response.message);
                }
                else {
                    $(targetDiv).html(response);
                }               
                UnBlockUI();
            },
            error: function (xhr) {
                UnBlockUI();
                if (xhr.status === 401) {
                    showToast("Bạn không có quyền truy cập! Vui lòng đăng nhập.");
                } else if (xhr.status === 403) {
                    showToast("Bạn không có quyền thực hiện thao tác này!");
                } else {
                    showToast("Không thể tải dữ liệu!");
                }
            }
        });
    },
    deleteData: function (controllerName,id) {
        if (confirm("Bạn có chắc chắn muốn xóa không?")) {
            BlockUI(); // Không cho người dùng nhập liệu khi đang thao tác với dữ liệu
            $.ajax({
                url: `/${controllerName}/Delete`,
                type: "POST",
                data: { id: id },
                success: function (res) {
                    UnBlockUI();
                    loaddata();
                    if (res.success) {
                        showToast(res.message);
                    }
                    else {
                        showToast(res.message);
                    }

                },
                error: function (xhr) {
                    UnBlockUI();
                    if (xhr.status === 401) {
                        showToast("Bạn không có quyền truy cập! Vui lòng đăng nhập.");
                    } else if (xhr.status === 403) {
                        showToast("Bạn không có quyền thực hiện thao tác này!");
                    } else {
                        showToast("Không thể tải dữ liệu!");
                    }
                }
            });
        } else {
            console.log("Hủy xóa!");
        }
    }
};
LoadModelSussess = function () {
    $("#myModal").modal("show");
    UnBlockUI();
}

successAction = function (res) {
    debugger;
    if (res.success) {
        UnBlockUI();
        $("#myModal").modal("hide");
        loaddata();
        showToast(res.message, "success");
    }
    else {
        UnBlockUI();
        showToast(res.message, "error");
    }
}


// Global counter
var blockUICount = 0;

function BlockUI() {
    blockUICount++;
    if (blockUICount === 1) {
        $("#blockUI").css("display", "flex");
    }
}

function UnBlockUI() {
    blockUICount--;
    if (blockUICount <= 0) {
        blockUICount = 0; // reset an toàn
        $("#blockUI").css("display", "none");
    }
}
