#!/usr/bin/env ruby
# coding: utf-8

# polico dsl の Cute 拡張
#
# dsl トップレベルで使える構文
#
#  cute_master_namespace master の namespace 指定
#  例） cute_master_namespace("CuteMaster")
#  cute_enum_namespace   enum 定義の namespace 指定
#  例） cute_enum_namespace("CuteEnum")
#
# enum 型定義の拡張
#
#  group の中でも enum 型の定義ができます。
#  ここで定義された enum 型は master で参照することができます。
#
# master スキーマのフィールド定義の拡張（型の追加）
#
#  float     polico の浮動小数は double 型のみ
#  localize  多言語対応
#  配列型     Unity のソースファイル生成用に対応。データ検証時は string 型として扱います。
#
#
# for_unity 内部で使える構文の拡張
#
#  cute_master_namespace master の namespace 指定
#                        トップレベルで指定された namespace から当該 master のみ変更します。
#  cute_original_type    配列型カラムのオリジナルの型情報
#                        配列型は sring に変換されるため、元の型を指定します。
#  例）フィールド名 fieldA の元の型が int[] の場合。
#     cute_original_type("fieldA", "int[]")

if __FILE__ == $0
require 'optparse'
    params = ARGV.getopts('p:', 'source_dsl:', 'verbose', 'command:', 'input:', 'output:', 'error_output:')
    puts  "ARGV.count #{ARGV.count}"

    polico = ""
    if params['p']
      polico = File::join(params['p'], "src/main.rb")
    end
    if File.exist?(polico)
      require polico
    else
      puts "-p <path-to-polico> is needed."
      exit (1)
    end
end

class DslParser
  class JailedParser
    def_delegator :@delegate, :cute_master_namespace
    def_delegator :@delegate, :cute_enum_namespace
    def_delegator :@delegate, :cute_csv_path
    def_delegator :@delegate, :cute_original_type
    def_delegator :@delegate, :float
    def_delegator :@delegate, :localize

    # 共通 enum 型定義を参照できるようにする
    alias :_orig_method_missing method_missing
    def method_missing(name, *arguments, &block)
      $logger.fatal "parse error where expecting symbol: illegal token '#{name.to_s}'" if block_given?
      if arguments.count > 0
        enum = @delegate._define_enum(name)
        $logger.fatal "parse error where expecting symbol: illegal token '#{name.to_s}'" if enum.nil?
        send(name.to_s, *arguments)
      else
        _orig_method_missing(name, *arguments, &block)
      end
    end
  end

  class ParserContext
    def cute_master_namespace(namespace)
      if @context == CONTEXT_TOPLEVEL
        @toplevel.namespace_of_master = namespace
      else
        _assert_context(CONTEXT_UNITY_PROXY)

        master = @group.last
        master.namespace_of_master = namespace
      end
    end

    def cute_enum_namespace(namespace)
      @toplevel.namespace_of_enum = namespace
    end

    def cute_csv_path(csv_path)
      _assert_context(CONTEXT_TOPLEVEL)
      @toplevel.csv_path = csv_path
    end

    def cute_original_type(fieldName, type)
      master = @group.last
      master.register_type_rename_proxy(fieldName.to_sym, type)
    end

    def float(name)
      item = double(name)
      cute_original_type(name, 'float')
      item
    end

    def localize(name)
      item = text(name)
      cute_original_type(name, 'localize')
      item
    end

    # group 直下に enum 型を定義できるようにする
    alias :_orig_enum :enum
    def enum(name)
      if @context == CONTEXT_MASTER
        _orig_enum(name)
      else
        raise "enum name cannot start with capital letter: #{name}" unless name.to_s[0] == name.to_s[0].downcase

        Proc.new do |body|
          @group.register_enum(name, body)
        end
      end
    end

    alias :_orig_push_master :_push_master
    def _push_master(name, attr)
      master = _orig_push_master(name, attr)
      master.namespace_of_master = @toplevel.namespace_of_master
      master.namespace_of_enum = @toplevel.namespace_of_enum
      master.csv_path = @toplevel.csv_path
      master
    end

    # 共通 enum 型を master 内で使用可能にする
    def _define_enum(name)
      if @group.enums.has_key?(name)
        master = @group.last
        master.register_enum(name, @group.enums[name])
        running_parser = JailedParser.class_eval("@@running")
        running_parser.define_singleton_method(name) do |spec|
          enum_item(master, name, spec)
        end
      else
        nil
      end
    end
  end
end

module StructuredDSL
  class SchemaItem

    def is_localized?
      map = @scheme.unity_proxy.type_rename_map
      map.has_key?(@name) && map[@name] == 'localize'
    end
  end

  class MasterPresentationProxy
    attr_reader :type_rename_map

    def register_type_rename_map(fieldName, type)
      $logger.fatal "duplicated rename source identifier: #{fieldName}" if @type_rename_map.has_key?(fieldName)
      @type_rename_map[fieldName] = type
    end

    alias :_orig_initialize :initialize
    def initialize
      _orig_initialize
      @type_rename_map = {}
    end

    alias :_orig_inherit :inherit
    def inherit(o)
      _orig_inherit(o)
      @type_rename_map = o.type_rename_map.dup
    end
  end

  class MasterSchema
    attr_accessor :namespace_of_master, :namespace_of_enum, :csv_path

    def register_type_rename_proxy(fieldName, type, target = :unity)
      proxy = _select_proxy(target)

      raise "undefined identifier for rename: #{fieldName}" unless @dic.has_key?(fieldName)
      proxy.register_type_rename_map(fieldName, type)
    end
  end

  # (group 直下に) master から参照可能な enum 型を定義できるようにする
  class MasterGroup
    attr_reader :enums

    def register_enum(name, body)
      @enums = {} if @enums.nil?
      raise "enum body must be a dictionary: #{name}" unless body.is_a?(Hash)
      raise "enum #{name} is already defined for this master" if @enums.has_key?(name)
      raise "empty enum does not make sense" if body.count == 0
      values = body.values
      raise "only integer enum is supported: #{body}" unless values.all?{ |v| v.to_i == v }
      raise "enum body has duplicated values: #{body}" unless values.uniq.count == values.count

      @enums[name] = body
    end
  end

  class TopLevel
    attr_accessor :namespace_of_master, :namespace_of_enum, :csv_path

  end
end

# *.cs ファイル名の先頭に 'Master' が付かないようにする
class UnityMasterClientTemplate

  alias :_orig_get_data_class_name :get_data_class_name
  def get_data_class_name(scheme)
    name = scheme.csv_path ?
      "Editor#{get_orm_class_name(scheme)}" :
      get_orm_class_name(scheme)
    _cute_master_group = scheme.csv_source.sub(/.csv$/, '') != scheme.qualified_name
    _cute_master_group ? name + "Group" : name
  end
end

# テンプレート読み込みパスを変更
class OutputTemplate

  alias :_orig_render :render
  def render(params)
    params[:template] = self

    erb = @erb_cache.load(File.join(File.dirname(__FILE__),
                                    (params.has_key?(:scheme) && params[:scheme].csv_path) ?
                                      'template_for_editor' :
                                      'template',
                                    self.class::TEMPLATE_PATH))
    ErbalT.new(params).render(erb)
  end

end

class UnityClientTemplate < OutputTemplate

  alias :_orig_rawtype_of_item :rawtype_of_item
  def rawtype_of_item(item)
    result = nil
    map = item.scheme.unity_proxy.type_rename_map
    if map.has_key?(item.name)
      result = map[item.name]
      if result == 'localize'
        result = 'string'
      end
    else
      result = _orig_rawtype_of_item(item)
    end
    result
  end
end

# '_' が連続する場合に対応
class String

  alias :_orig_camel_case :camel_case
  def camel_case(option = :uppper)
    self.gsub(/_+/, '_')._orig_camel_case(option)
  end
end

# 共通 enum 型定義を参照できるようにする
class MasterValidator

  alias :_orig_create_object_proxy :_create_object_proxy
  def _create_object_proxy(group)
    proxy = _orig_create_object_proxy(group)
    if group.is_a?(MasterGroup) and !group.enums.nil?
      group.enums.each do |name, body|
        proxy.define_singleton_method(name.to_sym) { OpenStruct.new(body)}
      end
    end
    proxy
  end
end

if __FILE__ == $0
    Encoding.default_external = 'UTF-8'

    # if ARGV.count <= 2
    #     show_usage_and_exit
    # end

    # params = ARGV.getopts('p:', 'source_dsl:', 'verbose', 'command:', 'input:', 'output:', 'error_output:')

    $logger = LoggerRepository.new(params["verbose"])
    $params = params;

    command = params["command"]
    aborted = false
    spell = nil

    begin
        case command
        when "gen_client"
            spell = gen_client(params)
        when "import"
            spell = import(params)
        when "validate"
            spell = validate(params)
        when "diff_csv"
            spell = diff_csv(params)
        else
            raise "unknown command: #{command}"
        end
    rescue Exception => e
        $logger.error e.inspect
        $logger.error e.backtrace.join("\n")
        aborted = true
    end

    error_output = params["error_output"]

    if not error_output.nil?
        begin
            $logger.output_report_html(error_output)
            $logger.flush
        rescue Exception => e
            $logger.error "failed to generate report html"
            $logger.output_report_html_last_resort(error_output, e.to_s + "<br>" + e.backtrace.unlines.gsub(/\n/, "<br>"))
        end
    end

    if aborted || $logger.total_errors_count > 0
        $logger.error "process failed due to reported errors."
        exit 1
    end

    $logger.puts ""
    $logger.puts spell
end
